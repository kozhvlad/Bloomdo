using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Client.Application.Services;

public class UsageSyncService(
    IAppUsageService? appUsageService,
    ILocalUsageStore localUsageStore,
    IStatsApiService statsApiService) : IUsageSyncService
{
    public async Task SaveLocalSnapshotAsync()
    {
        if (appUsageService is null) return;

        try
        {
            var usage = await appUsageService.GetTodayUsageAsync();
            var pickups = await appUsageService.GetPickupsTodayAsync();

            var snapshot = new LocalUsageSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                LastUpdatedUtc = DateTime.UtcNow,
                Pickups = pickups,
                SyncedToServer = false,
                Apps = usage.Select(u => new LocalAppUsageEntry
                {
                    PackageName = u.PackageName,
                    AppLabel = u.AppLabel,
                    ForegroundSeconds = (int)u.ForegroundTime.TotalSeconds
                }).ToList()
            };

            await localUsageStore.SaveSnapshotAsync(snapshot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveLocalSnapshot error: {ex}");
        }
    }

    public async Task SyncToServerAsync()
    {
        try
        {
            // Save fresh local snapshot first
            await SaveLocalSnapshotAsync();

            // Load from local cache (includes data that survived reboots)
            var today = DateOnly.FromDateTime(DateTime.Today);
            var snapshot = await localUsageStore.LoadSnapshotAsync(today);
            if (snapshot is null || snapshot.Apps.Count == 0) return;

            var request = BuildSyncRequest(snapshot);
            var success = await statsApiService.SyncUsageAsync(request);

            if (success)
                await localUsageStore.MarkSyncedAsync(today);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncToServer error: {ex}");
        }
    }

    public async Task SyncPendingAsync()
    {
        try
        {
            // First, save current device data locally in case it wasn't saved yet
            await SaveLocalSnapshotAsync();

            var unsynced = await localUsageStore.GetUnsyncedSnapshotsAsync();
            if (unsynced.Count == 0) return;

            foreach (var snapshot in unsynced)
            {
                if (snapshot.Apps.Count == 0) continue;

                try
                {
                    var request = BuildSyncRequest(snapshot);
                    var success = await statsApiService.SyncUsageAsync(request);

                    if (success)
                        await localUsageStore.MarkSyncedAsync(snapshot.Date);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SyncPending for {snapshot.Date} error: {ex}");
                    // Continue with other days even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncPending error: {ex}");
        }
    }

    private static SyncUsageRequest BuildSyncRequest(LocalUsageSnapshot snapshot) => new()
    {
        Date = snapshot.Date,
        Pickups = snapshot.Pickups,
        Apps = snapshot.Apps.Select(a => new AppUsageEntry
        {
            PackageName = a.PackageName,
            AppLabel = a.AppLabel,
            ForegroundSeconds = a.ForegroundSeconds
        }).ToList()
    };
}
