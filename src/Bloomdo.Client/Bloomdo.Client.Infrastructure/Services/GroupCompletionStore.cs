using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Infrastructure.Services;

public class GroupCompletionStore : IGroupCompletionStore
{
    private static readonly string FilePath = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "group_completion.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveCompletionStatusAsync(Dictionary<Guid, bool> groupCompletionStatus)
    {
        var json = JsonSerializer.Serialize(groupCompletionStatus, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task<Dictionary<Guid, bool>> LoadCompletionStatusAsync()
    {
        if (!File.Exists(FilePath))
            return [];

        var json = await File.ReadAllTextAsync(FilePath);
        return JsonSerializer.Deserialize<Dictionary<Guid, bool>>(json, JsonOptions) ?? [];
    }
}
