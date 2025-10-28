namespace AuthApp.Services.Models
{
    public record UserDto
    {
        public int Id { get; init; }
        public string? UserName { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string Password { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public IEnumerable<string> Roles { get; init; } = Array.Empty<string>();
    }
}
