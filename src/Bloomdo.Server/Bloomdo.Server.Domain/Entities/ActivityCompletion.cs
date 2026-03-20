namespace Bloomdo.Server.Domain.Entities;

public class ActivityCompletion : BaseEntity
{
    public Guid ActivityItemId { get; set; }
    public Guid AccountId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public int? CountValue { get; set; }
    public string? Note { get; set; }

    public ActivityItem ActivityItem { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
