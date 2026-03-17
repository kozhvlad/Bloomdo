namespace Bloomdo.Server.Domain.Entities;

public class Account : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }

    /// <summary>
    /// Serialized JSON avatar configuration.
    /// </summary>
    public string? AvatarJson { get; set; }

    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<AccountRole> AccountRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
