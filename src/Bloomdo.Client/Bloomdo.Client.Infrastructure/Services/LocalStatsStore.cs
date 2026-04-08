using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Client.Infrastructure.Services;

public class LocalStatsStore : ILocalStatsStore
{
    private static readonly string CacheDir = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "stats_cache");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly SemaphoreSlim Lock = new(1, 1);

    // ── Month Calendar ──────────────────────────────────────────

    public Task SaveMonthCalendarAsync(int year, int month, MonthCalendarResponse data)
        => WriteAsync($"calendar_{year:D4}_{month:D2}.json", data);

    public Task<MonthCalendarResponse?> LoadMonthCalendarAsync(int year, int month)
        => ReadAsync<MonthCalendarResponse>($"calendar_{year:D4}_{month:D2}.json");

    // ── Weekly Stats ────────────────────────────────────────────

    public Task SaveWeeklyStatsAsync(DateOnly weekStart, WeeklyStatsResponse data)
        => WriteAsync($"weekly_{weekStart:yyyy_MM_dd}.json", data);

    public Task<WeeklyStatsResponse?> LoadWeeklyStatsAsync(DateOnly weekStart)
        => ReadAsync<WeeklyStatsResponse>($"weekly_{weekStart:yyyy_MM_dd}.json");

    // ── Daily Stats ─────────────────────────────────────────────

    public Task SaveDailyStatsAsync(DateOnly date, DailyStatsResponse data)
        => WriteAsync($"daily_{date:yyyy_MM_dd}.json", data);

    public Task<DailyStatsResponse?> LoadDailyStatsAsync(DateOnly date)
        => ReadAsync<DailyStatsResponse>($"daily_{date:yyyy_MM_dd}.json");

    // ── Cleanup ─────────────────────────────────────────────────

    public Task CleanupAsync(int days = 30)
    {
        EnsureCacheDir();
        var cutoff = DateTime.UtcNow.AddDays(-days);

        try
        {
            foreach (var file in Directory.GetFiles(CacheDir, "*.json"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    try { File.Delete(file); } catch { /* skip */ }
                }
            }
        }
        catch
        {
            // Directory may not exist yet
        }

        return Task.CompletedTask;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private async Task WriteAsync<T>(string fileName, T data)
    {
        EnsureCacheDir();
        var filePath = Path.Combine(CacheDir, fileName);

        await Lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    private async Task<T?> ReadAsync<T>(string fileName) where T : class
    {
        var filePath = Path.Combine(CacheDir, fileName);
        if (!File.Exists(filePath))
            return null;

        await Lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
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

    private static void EnsureCacheDir()
    {
        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
    }
}
