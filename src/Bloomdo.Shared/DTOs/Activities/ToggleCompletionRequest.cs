namespace Bloomdo.Shared.DTOs.Activities;

public class ToggleCompletionRequest
{
    public Guid ActivityItemId { get; set; }
    public DateOnly Date { get; set; }
    public int? CountValue { get; set; }
    public string? Note { get; set; }
}
