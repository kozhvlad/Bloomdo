using Bloomdo.Shared.DTOs.Subscription;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Persists the latest subscription status locally so premium checks
/// (colors, emoji, block limits, stats access) work while offline.
/// </summary>
public interface ILocalSubscriptionStore
{
    Task SaveAsync(SubscriptionStatusResponse status);
    Task<SubscriptionStatusResponse?> LoadAsync();
}
