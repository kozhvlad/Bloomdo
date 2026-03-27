namespace Bloomdo.Shared.DTOs.Activities;

public class VerifyPhotoResponse
{
    public VerificationStatus Status { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
