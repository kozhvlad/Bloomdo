using Bloomdo.Server.Application.Settings;

namespace Bloomdo.Server.Infrastructure.Settings;

public class FreeLimitsSettings : IFreeLimitsSettings
{
    public int MaxDailyChatMessages { get; set; } = 10;
    public int MaxBlockRules { get; set; } = 3;
    public bool CanCustomizeEmoji { get; set; } = false;
    public bool CanCustomizeColors { get; set; } = false;
    public int MonthlyStreakFreezes { get; set; } = 2;
    public bool CanViewWeeklyStats { get; set; } = false;
}
