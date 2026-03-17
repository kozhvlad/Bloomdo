using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Auth;

public class AccountProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public AvatarConfig? Avatar { get; set; }
    public List<UserRole> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
