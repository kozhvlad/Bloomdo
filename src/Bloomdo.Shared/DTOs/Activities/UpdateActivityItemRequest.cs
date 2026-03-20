namespace Bloomdo.Shared.DTOs.Activities;

public class UpdateActivityItemRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ActivityItemType? TaskType { get; set; }
    public int? DurationMinutes { get; set; }
    public int? TargetCount { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
