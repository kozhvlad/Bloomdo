using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Infrastructure.Services;

public class LocalUsageStore : ILocalUsageStore
{
    private static readonly string CacheDir = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "usage_cache");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task SaveSnapshotAsync(LocalUsageSnapshot snapshot)
    {
        EnsureCacheDir();
        var filePath = GetFilePath(snapshot.Date);

        await Lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<LocalUsageSnapshot?> LoadSnapshotAsync(DateOnly date)
    {
        var filePath = GetFilePath(date);
        if (!File.Exists(filePath))
            return null;

        await Lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<LocalUsageSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<IReadOnlyList<LocalUsageSnapshot>> GetUnsyncedSnapshotsAsync()
    {
        EnsureCacheDir();
        var result = new List<LocalUsageSnapshot>();

        await Lock.WaitAsync();
        try
        {
            var files = Directory.GetFiles(CacheDir, "usage_*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var snapshot = JsonSerializer.Deserialize<LocalUsageSnapshot>(json, JsonOptions);
                    if (snapshot is not null && !snapshot.SyncedToServer)
                        result.Add(snapshot);
                }
                catch
                {
                    // Skip corrupted files
                }
            }
        }
        finally
        {
            Lock.Release();
        }

        return result;
    }

    public async Task MarkSyncedAsync(DateOnly date)
    {
        var snapshot = await LoadSnapshotAsync(date);
        if (snapshot is null) return;

        snapshot.SyncedToServer = true;
        await SaveSnapshotAsync(snapshot);
    }

    /// <summary>
    /// Static helper for use from foreground services that don't have DI access.
    /// Saves usage data directly to the cache file.
    /// </summary>
    public static void SaveSnapshotDirect(DateOnly date, int pickups, List<LocalAppUsageEntry> apps)
    {
        try
        {
            EnsureCacheDir();
            var filePath = GetFilePath(date);

            // Read existing to preserve SyncedToServer flag
            LocalUsageSnapshot? existing = null;
            if (File.Exists(filePath))
            {
                try
                {
                    var existingJson = File.ReadAllText(filePath);
                    existing = JsonSerializer.Deserialize<LocalUsageSnapshot>(existingJson, JsonOptions);
                }
                catch
                {
                    // Ignore parse errors
                }
            }

            var snapshot = new LocalUsageSnapshot
            {
                Date = date,
                LastUpdatedUtc = DateTime.UtcNow,
                Pickups = pickups,
                SyncedToServer = false, // New data means we need to re-sync
                Apps = apps
            };

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Best-effort save — don't crash the foreground service
        }
    }

    /// <summary>
    /// Removes cache files older than the specified number of days.
    /// </summary>
    public static void CleanupOldFiles(int keepDays = 14)
    {
        try
        {
            if (!Directory.Exists(CacheDir)) return;

            var cutoff = DateOnly.FromDateTime(DateTime.Today).AddDays(-keepDays);
            var files = Directory.GetFiles(CacheDir, "usage_*.json");

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // usage_2024-01-15
                if (fileName.Length >= 16 &&
                    DateOnly.TryParseExact(fileName[6..], "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var fileDate) &&
                    fileDate < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    private static string GetFilePath(DateOnly date)
        => Path.Combine(CacheDir, $"usage_{date:yyyy-MM-dd}.json");

    private static void EnsureCacheDir()
    {
        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
    }
}
