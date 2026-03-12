using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Client.Infrastructure.Services;

public class BlockApiService(HttpClient httpClient) : IBlockApiService
{
    public async Task<List<BlockRuleResponse>?> GetBlockRulesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(ApiRoutes.Blocks.List, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<BlockRuleResponse>>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetBlockRules failed: {ex.Message}");
            return null;
        }
    }

    public async Task<BlockRuleResponse?> CreateBlockRuleAsync(CreateBlockRuleRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.Blocks.Create, request, ct);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<BlockRuleResponse>(ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");
    }

    public async Task<BlockRuleResponse?> UpdateBlockRuleAsync(Guid id, UpdateBlockRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/blocks/{id}";
            var response = await httpClient.PutAsJsonAsync(url, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<BlockRuleResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateBlockRule failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteBlockRuleAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/blocks/{id}";
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteBlockRule failed: {ex.Message}");
            return false;
        }
    }
}
