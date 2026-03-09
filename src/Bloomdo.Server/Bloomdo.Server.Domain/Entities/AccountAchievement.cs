namespace Bloomdo.Server.Domain.Entities;

public class AccountAchievement : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid AchievementId { get; set; }
    public DateOnly UnlockedDate { get; set; }

    public Account Account { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}
