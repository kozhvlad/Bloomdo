namespace Bloomdo.Shared.DTOs.Profile;

public class ProfileStatsResponse
{
    public int StreakDays { get; set; }
    public int TasksCompleted { get; set; }
    public int FocusHours { get; set; }
    public int TotalBlocksCreated { get; set; }
    public int AchievementsUnlocked { get; set; }
    public DateTime JoinedAt { get; set; }
}
