namespace AuthApp.Controllers
{
    public partial class UserManagementController
    {
        // Request DTOs local to this controller
        public record CreateUserRequest(string Email, string Password, string[] Roles);
    }
}
