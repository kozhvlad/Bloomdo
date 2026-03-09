namespace Bloomdo.Server.Domain.Entities;

public class Achievement : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<AccountAchievement> AccountAchievements { get; set; } = [];
}
