namespace Bloomdo.Shared.DTOs.Profile;

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public AvatarConfig? Avatar { get; set; }
}
