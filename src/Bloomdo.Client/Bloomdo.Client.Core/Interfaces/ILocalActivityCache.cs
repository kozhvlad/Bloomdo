using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Locally caches daily activity data and queues offline mutations.
/// </summary>
public interface ILocalActivityCache
{
    Task SaveDailyAsync(DailyActivitiesResponse daily, DateOnly date);
    Task<DailyActivitiesResponse?> LoadDailyAsync(DateOnly date);
    Task EnqueueToggleAsync(PendingActivityToggle toggle);
    Task<IReadOnlyList<PendingActivityToggle>> LoadPendingTogglesAsync();
    Task ClearPendingTogglesAsync();
}

/// <summary>
/// Represents an offline-queued completion toggle to be synced later.
/// </summary>
public sealed class PendingActivityToggle
{
    public Guid ActivityItemId { get; set; }
    public DateOnly Date { get; set; }
    public int? CountValue { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
