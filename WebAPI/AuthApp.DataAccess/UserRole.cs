namespace AuthApp.DataAccess
{
    // Explicit join entity for many-to-many relationship between User and Role.
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
}
