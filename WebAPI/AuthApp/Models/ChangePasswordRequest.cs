﻿namespace AuthApp.Models
{
    // DTO for password change
    public record ChangePasswordRequest
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
