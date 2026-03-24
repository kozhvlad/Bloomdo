namespace Bloomdo.Server.Application.Settings;

public interface IFreeLimitsSettings
{
    int MaxDailyChatMessages { get; }
    int MaxBlockRules { get; }
    bool CanCustomizeEmoji { get; }
    bool CanCustomizeColors { get; }
    int MonthlyStreakFreezes { get; }
    bool CanViewWeeklyStats { get; }
}
