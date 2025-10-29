using AuthApp.DataAccess;
using AuthApp.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AuthApp.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly DBContext _db;
        private readonly ILogger<UserManagementService>? _logger;

        public UserManagementService(DBContext db, ILogger<UserManagementService>? logger = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Set<User>()
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(u => u.Id == id, ct)
                                  .ConfigureAwait(false);
            return entity == null ? null : entity.ToDto();
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalized = email.Trim().ToLowerInvariant();
            var entity = await _db.Set<User>()
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct)
                                  .ConfigureAwait(false);
            return entity == null ? null : entity.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
        {
            var entities = await _db.Set<User>()
                                    .AsNoTracking()
                                    .ToListAsync(ct)
                                    .ConfigureAwait(false);

            var userRolePairs = await (from ur in _db.Set<UserRole>().AsNoTracking()
                                       join r in _db.Set<Role>().AsNoTracking() on ur.RoleId equals r.Id
                                       select new { ur.UserId, RoleName = r.Name })
                                      .ToListAsync(ct)
                                      .ConfigureAwait(false);

            var rolesByUser = userRolePairs
                              .Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                              .GroupBy(x => x.UserId)
                              .ToDictionary(g => g.Key,
                                            g => (IEnumerable<string>)g.Select(x => x.RoleName!.Trim())
                                                                   .Where(n => !string.IsNullOrWhiteSpace(n))
                                                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                                                   .ToList());

            var result = entities.Select(e =>
            {
                var dto = e.ToDto();
                var roles = rolesByUser.TryGetValue(e.Id, out var r) ? r : Enumerable.Empty<string>();
                return dto with { Roles = roles };
            }).ToList();

            return result;
        }

        public async Task<UserDto> CreateUserAsync(string email, string password, string[] roles, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("password required", nameof(password));

            var normalized = email.Trim().ToLowerInvariant();
            var exists = await _db.Set<User>()
                                  .AsNoTracking()
                                  .AnyAsync(u => u.Email.ToLower() == normalized, ct)
                                  .ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException("A user with that email already exists.");
            }

            var user = new User
            {
                UserName = email,
                Email = email.Trim(),
                Password = password,
                PasswordHash = Helpers.ComputeSha256Hash(password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Set<User>().Add(user);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            // Assign roles if provided
            if (roles != null && roles.Length > 0)
            {
                var roleEntities = await _db.Set<Role>()
                                            .AsNoTracking()
                                            .Where(r => roles.Contains(r.Name))
                                            .ToListAsync(ct)
                                            .ConfigureAwait(false);
                foreach (var role in roleEntities)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    };
                    _db.Set<UserRole>().Add(userRole);
                }
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger?.LogInformation("Created user {UserId} ({Email})", user.Id, user.Email);
            return user.ToDto();
        }

        public async Task<bool> UpdateUserAsync(UserDto userDto, CancellationToken ct = default)
        {
            if (userDto == null) throw new ArgumentNullException(nameof(userDto));

            var entity = await _db.Set<User>()
                                  .FirstOrDefaultAsync(u => u.Id == userDto.Id, ct)
                                  .ConfigureAwait(false);
            if (entity == null) return false;

            // Apply basic DTO fields to entity (username, email, password, isActive)
            ApplyDtoToEntity(userDto, entity);

            // Flag indicates whether roles should be synchronized.
            var syncRoles = userDto.Roles != null;

            // Prepare normalized requested role names if roles are provided
            List<string> requestedNormalized = new();
            if (syncRoles)
            {
                requestedNormalized = NormalizeRequestedRoles(userDto.Roles);
            }

            // Delegate update + role synchronization to extracted method
            return await UpdateEntityAndSyncRolesAsync(entity, userDto, syncRoles, requestedNormalized, ct).ConfigureAwait(false);
        }

        private async Task<bool> UpdateEntityAndSyncRolesAsync(User entity, UserDto userDto, bool syncRoles, List<string> requestedNormalized, CancellationToken ct)
        {
            try
            {
                // If roles should be updated, delegate synchronization to extracted method
                if (syncRoles)
                {
                    await SyncRolesAsync(entity.Id, requestedNormalized, ct).ConfigureAwait(false);
                }

                // Update user entity and save all changes (user + role changes) in one transaction
                _db.Set<User>().Update(entity);

                var changed = await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                if (syncRoles)
                {
                    _logger?.LogInformation("Updated user {UserId} and synchronized roles (requested count: {RequestedCount})", userDto.Id, requestedNormalized.Count);
                }
                else
                {
                    _logger?.LogInformation("Updated user {UserId} (roles not modified)", userDto.Id);
                }

                return changed > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger?.LogWarning(ex, "Concurrency conflict updating user {UserId}", userDto.Id);
                return false;
            }
        }

        // Extracted helper to compute role diff and apply additions/removals
        private async Task SyncRolesAsync(int userId, List<string> requestedNormalized, CancellationToken ct)
        {
            // Load existing user-role pairs (tracked) along with role names
            var existing = await LoadExistingUserRolesAsync(userId, ct).ConfigureAwait(false);

            // Build set of existing role names
            var existingNames = GetExistingRoleNames(existing);

            // Build requested set (case-insensitive)
            var requestedSet = requestedNormalized
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Determine names to add and remove
            var toAdd = requestedSet.Except(existingNames, StringComparer.OrdinalIgnoreCase).ToList();
            var toRemove = existingNames.Except(requestedSet, StringComparer.OrdinalIgnoreCase).ToList();

            if (toAdd.Count > 0)
            {
                await AddRolesAsync(userId, existingNames, toAdd, ct).ConfigureAwait(false);
            }

            if (toRemove.Count > 0)
            {
                RemoveRoles(existing, toRemove);
            }
        }

        public async Task<bool> DeleteUserAsync(int id, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct).ConfigureAwait(false);
            if (user == null) return false;

            _db.Set<User>().Remove(user);
            var changed = await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger?.LogInformation("Deleted user {UserId}", id);
            return changed > 0;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email) || password == null) return false;

            // load entity (no tracking not necessary because we won't modify it)
            var user = await _db.Set<User>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLowerInvariant(), ct)
                                .ConfigureAwait(false);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return false;

            var valid = VerifyPassword(user.PasswordHash, password);
            if (!valid)
            {
                _logger?.LogInformation("Failed login attempt for {Email}", email);
            }
            return valid;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(newPassword)) throw new ArgumentException("newPassword required", nameof(newPassword));
            if (string.IsNullOrEmpty(currentPassword)) return false;

            var user = await _db.Set<User>()
                                .FirstOrDefaultAsync(u => u.Id == userId, ct)
                                .ConfigureAwait(false);
            if (user == null) return false;

            // Verify current password
            if (!VerifyPassword(user.PasswordHash, currentPassword))
            {
                _logger?.LogInformation("Failed password change attempt for user {UserId}", userId);
                return false;
            }

            var newHash = Helpers.ComputeSha256Hash(newPassword);
            if (string.Equals(newHash, user.PasswordHash, StringComparison.Ordinal)) return false;

            user.PasswordHash = newHash;

            try
            {
                _db.Set<User>().Update(user);
                var changed = await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                if (changed > 0)
                {
                    _logger?.LogInformation("Password changed for user {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger?.LogWarning(ex, "Concurrency conflict changing password for user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string username, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username)) return Enumerable.Empty<string>();

            // Ensure user exists (avoid returning roles for non-existent users)
            var user = await _db.Set<User>()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.Email.ToLower() == username.Trim().ToLowerInvariant(), ct)
                                 .ConfigureAwait(false);
            if (user == null) return Enumerable.Empty<string>();

            // Join UserRole -> Role to fetch role names
            var roleNames = await (from ur in _db.Set<UserRole>().AsNoTracking()
                                   join r in _db.Set<Role>().AsNoTracking() on ur.RoleId equals r.Id
                                   where ur.UserId == user.Id
                                   select r.Name)
                                  .ToListAsync(ct)
                                  .ConfigureAwait(false);

            var filtered = roleNames
                           .Where(n => !string.IsNullOrWhiteSpace(n))
                           .Select(n => n!.Trim())
                           .ToList();

            _logger?.LogDebug("Fetched {Count} roles for user {username}", filtered.Count, username);

            return filtered;
        }

        private static void ApplyDtoToEntity(UserDto dto, User entity)
        {
            entity.UserName = dto.UserName ?? entity.UserName;
            entity.Password = dto.Password;
            entity.PasswordHash = Helpers.ComputeSha256Hash(dto.Password);
            entity.Email = string.IsNullOrWhiteSpace(dto.Email) ? entity.Email : dto.Email.Trim();
            entity.IsActive = dto.IsActive;
        }

        private static bool VerifyPassword(string hashed, string password)
        {
            if (string.IsNullOrEmpty(hashed) || password == null) return false;

            try
            {
                var candidateHex = Helpers.ComputeSha256Hash(password);
                var a = System.Text.Encoding.UTF8.GetBytes(candidateHex);
                var b = System.Text.Encoding.UTF8.GetBytes(hashed);

                if (a.Length != b.Length) return false;
                return CryptographicOperations.FixedTimeEquals(a, b);
            }
            catch
            {
                return false;
            }
        }

        private static List<string> NormalizeRequestedRoles(IEnumerable<string>? roles)
        {
            if (roles == null) return new List<string>();
            return roles
                   .Where(r => !string.IsNullOrWhiteSpace(r))
                   .Select(r => r!.Trim())
                   .Where(r => r.Length > 0)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();
        }

        private async Task<List<(UserRole UserRole, string? RoleName)>> LoadExistingUserRolesAsync(int userId, CancellationToken ct)
        {
            var query = await (from ur in _db.Set<UserRole>().AsTracking()
                               join r in _db.Set<Role>().AsNoTracking() on ur.RoleId equals r.Id
                               where ur.UserId == userId
                               select new { UserRole = ur, RoleName = r.Name })
                              .ToListAsync(ct)
                              .ConfigureAwait(false);

            return query.Select(x => (x.UserRole, x.RoleName)).ToList();
        }

        private static HashSet<string> GetExistingRoleNames(IEnumerable<(UserRole UserRole, string? RoleName)> existing)
        {
            return existing
                   .Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                   .Select(x => x.RoleName!.Trim())
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private async Task AddRolesAsync(int userId, HashSet<string> existingNames, List<string> toAdd, CancellationToken ct)
        {
            var toAddLower = toAdd.Select(n => n.ToLowerInvariant()).ToList();
            var roleEntities = await _db.Set<Role>()
                                        .AsNoTracking()
                                        .Where(r => toAddLower.Contains(r.Name.ToLower()))
                                        .ToListAsync(ct)
                                        .ConfigureAwait(false);

            foreach (var role in roleEntities)
            {
                // Ensure we haven't already got this assignment (defensive)
                if (!existingNames.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
                {
                    _db.Set<UserRole>().Add(new UserRole
                    {
                        UserId = userId,
                        RoleId = role.Id
                    });
                }
            }
        }

        private void RemoveRoles(IEnumerable<(UserRole UserRole, string? RoleName)> existing, List<string> toRemove)
        {
            var toRemoveLower = toRemove.Select(n => n.ToLowerInvariant()).ToList();

            var removals = existing
                           .Where(x => x.RoleName != null && toRemoveLower.Contains(x.RoleName!.Trim().ToLowerInvariant()))
                           .Select(x => x.UserRole)
                           .ToList();

            if (removals.Count > 0)
            {
                _db.Set<UserRole>().RemoveRange(removals);
            }
        }
    }
}
