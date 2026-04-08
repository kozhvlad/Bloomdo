using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.Infrastructure.Services;

public class LocalActivityCache : ILocalActivityCache
{
    private static readonly string CacheDir = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "activity_cache");

    private static readonly string PendingFile = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "pending_toggles.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task SaveDailyAsync(DailyActivitiesResponse daily, DateOnly date)
    {
        EnsureCacheDir();
        var filePath = GetFilePath(date);

        await Lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(daily, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<DailyActivitiesResponse?> LoadDailyAsync(DateOnly date)
    {
        var filePath = GetFilePath(date);
        if (!File.Exists(filePath))
            return null;

        await Lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<DailyActivitiesResponse>(json, JsonOptions);
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

    public async Task EnqueueToggleAsync(PendingActivityToggle toggle)
    {
        await Lock.WaitAsync();
        try
        {
            var pending = await LoadPendingInternalAsync();
            pending.Add(toggle);
            var json = JsonSerializer.Serialize(pending, JsonOptions);
            await File.WriteAllTextAsync(PendingFile, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<IReadOnlyList<PendingActivityToggle>> LoadPendingTogglesAsync()
    {
        await Lock.WaitAsync();
        try
        {
            return await LoadPendingInternalAsync();
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task ClearPendingTogglesAsync()
    {
        await Lock.WaitAsync();
        try
        {
            if (File.Exists(PendingFile))
                File.Delete(PendingFile);
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task<List<PendingActivityToggle>> LoadPendingInternalAsync()
    {
        if (!File.Exists(PendingFile))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(PendingFile);
            return JsonSerializer.Deserialize<List<PendingActivityToggle>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string GetFilePath(DateOnly date) => Path.Combine(CacheDir, $"daily_{date:yyyy-MM-dd}.json");

    private static void EnsureCacheDir()
    {
        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
    }
}
