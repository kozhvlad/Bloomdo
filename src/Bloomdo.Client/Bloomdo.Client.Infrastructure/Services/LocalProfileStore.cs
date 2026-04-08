using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Infrastructure.Services;

public class LocalProfileStore : ILocalProfileStore
{
    private static readonly string FilePath = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "profile_cache", "profile.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task SaveAsync(LocalProfileSnapshot snapshot)
    {
        EnsureCacheDir();

        await Lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(FilePath, json);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<LocalProfileSnapshot?> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        await Lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<LocalProfileSnapshot>(json, JsonOptions);
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
        var dir = Path.GetDirectoryName(FilePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
