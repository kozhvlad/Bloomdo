namespace Bloomdo.Shared.DTOs.Activities;

public class VerifyPhotoRequest
{
    public Guid ActivityItemId { get; set; }
    public DateOnly Date { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
}
