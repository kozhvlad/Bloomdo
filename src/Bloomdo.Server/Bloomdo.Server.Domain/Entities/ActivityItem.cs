namespace Bloomdo.Server.Domain.Entities;

public class ActivityItem : BaseEntity
{
    public Guid ActivityGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TaskType { get; set; }
    public int? DurationMinutes { get; set; }
    public int? TargetCount { get; set; }
    public string Icon { get; set; } = "✨";
    public string Color { get; set; } = "#7E57C2";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? VerificationTemplateId { get; set; }
    public string? CustomVerificationCriteria { get; set; }

    public ActivityGroup Group { get; set; } = null!;
    public ICollection<ActivityCompletion> Completions { get; set; } = [];
}
