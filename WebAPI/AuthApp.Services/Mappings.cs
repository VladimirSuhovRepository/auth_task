using AuthApp.DataAccess;
using AuthApp.Services.Models;

namespace AuthApp.Services
{
    public static class Mappings
    {
        public static UserDto ToDto(this User u) =>
            new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Password = u.Password
            };
    }
}
