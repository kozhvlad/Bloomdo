namespace Bloomdo.Shared.DTOs.Subscription;

public sealed class SubscriptionLimitsDto
{
    public int MaxDailyChatMessages { get; set; }
    public int RemainingChatMessagesToday { get; set; }

    public int MaxBlockRules { get; set; }
    public int CurrentBlockRuleCount { get; set; }

    public bool CanCustomizeEmoji { get; set; }
    public bool CanCustomizeColors { get; set; }

    public int MonthlyStreakFreezes { get; set; }
    public int RemainingStreakFreezes { get; set; }

    public bool CanViewWeeklyStats { get; set; }
}
