namespace Bloomdo.Shared.DTOs.Activities;

public class DailyActivitiesResponse
{
    public DateOnly Date { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public List<DailyActivityGroupDto> Groups { get; set; } = [];
}

public class DailyActivityGroupDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int CurrentStreak { get; set; }
    public List<DailyActivityItemDto> Items { get; set; } = [];
}

public class DailyActivityItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityItemType TaskType { get; set; }
    public int? DurationMinutes { get; set; }
    public int? TargetCount { get; set; }
    public int CurrentCount { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
