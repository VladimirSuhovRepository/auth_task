using AuthApp.Services.Models;

namespace AuthApp.Services
{
    public interface IUserManagementService
    {
        Task<UserDto> CreateUserAsync(string email, string password, string[] roles, CancellationToken ct = default);
        Task<bool> DeleteUserAsync(int id, CancellationToken ct = default);
        Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default);
        Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetUserRolesAsync(string username, CancellationToken ct = default);
        Task<bool> UpdateUserAsync(UserDto user, CancellationToken ct = default);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);
        Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    }
}