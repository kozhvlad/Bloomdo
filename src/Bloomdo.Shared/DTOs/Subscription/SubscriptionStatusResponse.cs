using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Subscription;

public class SubscriptionStatusResponse
{
    public SubscriptionStatus Status { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool IsPremium { get; set; }
    public bool WillCancel { get; set; }
    public SubscriptionLimitsDto Limits { get; set; } = new();
}
