namespace Bloomdo.Shared.DTOs.Activities;

public class ActivityItemResponse
{
    public Guid Id { get; set; }
    public Guid ActivityGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityItemType TaskType { get; set; }
    public int? DurationMinutes { get; set; }
    public int? TargetCount { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public VerificationTemplate? VerificationTemplate { get; set; }
    public string? CustomVerificationCriteria { get; set; }
}
