namespace Bloomdo.Server.Domain.Entities;

public class StreakFreeze : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    /// <summary>
    /// The date that was "frozen" — the streak continues through this day.
    /// </summary>
    public DateOnly Date { get; set; }
}
