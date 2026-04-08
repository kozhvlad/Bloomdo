namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Orchestrates reading usage from device, saving locally, and syncing to server.
/// </summary>
public interface IUsageSyncService
{
    /// <summary>
    /// Reads current usage from device and saves a local snapshot.
    /// Cheap operation — safe to call frequently.
    /// </summary>
    Task SaveLocalSnapshotAsync();

    /// <summary>
    /// Reads current usage, saves locally, and pushes today's data to the server.
    /// </summary>
    Task SyncToServerAsync();

    /// <summary>
    /// Finds all locally cached days that haven't been synced to the server and pushes them.
    /// Call on app startup to recover data from previous sessions.
    /// </summary>
    Task SyncPendingAsync();
}
