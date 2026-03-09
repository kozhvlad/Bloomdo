namespace Bloomdo.Server.Domain.Entities;

public class DailySnapshot : BaseEntity
{
    public Guid AccountId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalScreenTimeSeconds { get; set; }
    public int Pickups { get; set; }
    public bool GoalMet { get; set; }

    public Account Account { get; set; } = null!;
}
