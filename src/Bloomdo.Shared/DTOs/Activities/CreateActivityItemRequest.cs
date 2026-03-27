namespace Bloomdo.Shared.DTOs.Activities;

public class CreateActivityItemRequest
{
    public Guid ActivityGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityItemType TaskType { get; set; } = ActivityItemType.Timer;
    public int? DurationMinutes { get; set; }
    public int? TargetCount { get; set; }
    public string Icon { get; set; } = "✨";
    public string Color { get; set; } = "#7E57C2";
    public VerificationTemplate? VerificationTemplate { get; set; }
    public string? CustomVerificationCriteria { get; set; }
}
