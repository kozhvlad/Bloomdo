using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Infrastructure.Services;

public class LocalTimerStateStore : ITimerStateStore
{
    private static readonly string StoreDir = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "timer_state");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task SaveAsync(TimerStateSnapshot state)
    {
        EnsureDir();
        var filePath = GetFilePath(state.TaskId);

        await Lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<TimerStateSnapshot?> LoadAsync(Guid taskId)
    {
        var filePath = GetFilePath(taskId);
        if (!File.Exists(filePath))
            return null;

        await Lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var state = JsonSerializer.Deserialize<TimerStateSnapshot>(json, JsonOptions);

            if (state is null)
                return null;

            // Auto-expire: only return if the saved state is from today
            if (state.Date != DateOnly.FromDateTime(DateTime.UtcNow))
            {
                File.Delete(filePath);
                return null;
            }

            return state;
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

    public async Task ClearAsync(Guid taskId)
    {
        var filePath = GetFilePath(taskId);

        await Lock.WaitAsync();
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        finally
        {
            Lock.Release();
        }
    }

    private static string GetFilePath(Guid taskId) =>
        Path.Combine(StoreDir, $"timer_{taskId:N}.json");

    private static void EnsureDir()
    {
        if (!Directory.Exists(StoreDir))
            Directory.CreateDirectory(StoreDir);
    }
}
