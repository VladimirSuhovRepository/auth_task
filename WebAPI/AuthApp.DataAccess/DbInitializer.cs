using Microsoft.Extensions.DependencyInjection;

namespace AuthApp.DataAccess
{
    public static class DbInitializer
    {
        /// <summary>
        /// Ensure database is migrated and seeded.
        /// Call this at startup: DbInitializer.EnsureSeedData(app.Services);
        /// </summary>
        public static void EnsureSeedData(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var context = provider.GetRequiredService<DBContext>();

            // Fallback programmatic seeding in case migrations/HasData were not applied or for fresh DBs.
            // This is idempotent: it checks existence by natural keys.
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "User" }
                );
                context.SaveChanges();
            }

            // Seed users if missing
            if (!context.Users.Any(u => u.UserName == "admin"))
            {
                var createdAt = new DateTime(2025, 10, 28, 0, 0, 0, DateTimeKind.Utc);
                var admin = new User
                {
                    UserName = "admin",
                    Email = "admin@task.com",
                    Password = "Admin123!",
                    PasswordHash = Helpers.ComputeSha256Hash("Admin123!"),
                    IsActive = true,
                    CreatedAt = createdAt
                };

                var user = new User
                {
                    UserName = "user",
                    Email = "user@task.com",
                    Password = "User123!",
                    PasswordHash = Helpers.ComputeSha256Hash("User123!"),
                    IsActive = true,
                    CreatedAt = createdAt
                };

                context.Users.AddRange(admin, user);
                context.SaveChanges();
            }

            // --- Initialize role assignments for the seeded users ---
            // Load roles and users from DB (fresh instances/tracked)
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = context.Roles.FirstOrDefault(r => r.Name == "User");

            if (adminRole == null || userRole == null)
            {
                // Roles missing for some reason; nothing to assign.
                return;
            }

            var adminUser = context.Users.FirstOrDefault(u => u.UserName == "admin");
            var normalUser = context.Users.FirstOrDefault(u => u.UserName == "user");

            if (normalUser == null || normalUser == null)
            {
                // User is missing; nothing to assign.
                return;
            }

            // Assign roles to users
            context.UserRoles.AddRange(
                new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
                new UserRole { UserId = normalUser.Id, RoleId = userRole.Id }
            );
            context.SaveChanges();
        }
    }
}
