using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Auth;

public class AuthResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<UserRole> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}
