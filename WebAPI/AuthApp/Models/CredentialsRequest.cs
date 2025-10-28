namespace AuthApp.Controllers
{
    public partial class UserManagementController
    {
        public record CredentialsRequest(string Email, string Password);
    }
}
