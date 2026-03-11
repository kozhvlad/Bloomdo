using System.Text.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Client.Infrastructure.Services;

public class BlockRuleStore : IBlockRuleStore
{
    private static readonly string FilePath = Path.Combine(
        Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "block_rules.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveRulesAsync(IReadOnlyList<BlockRuleResponse> rules)
    {
        var json = JsonSerializer.Serialize(rules, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task<IReadOnlyList<BlockRuleResponse>> LoadRulesAsync()
    {
        if (!File.Exists(FilePath))
            return [];

        var json = await File.ReadAllTextAsync(FilePath);
        return JsonSerializer.Deserialize<List<BlockRuleResponse>>(json, JsonOptions) ?? [];
    }
}
