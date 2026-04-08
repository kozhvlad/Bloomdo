using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Persists daily usage snapshots locally so data survives app/device restarts.
/// </summary>
public interface ILocalUsageStore
{
    Task SaveSnapshotAsync(LocalUsageSnapshot snapshot);
    Task<LocalUsageSnapshot?> LoadSnapshotAsync(DateOnly date);
    Task<IReadOnlyList<LocalUsageSnapshot>> GetUnsyncedSnapshotsAsync();
    Task MarkSyncedAsync(DateOnly date);
}
