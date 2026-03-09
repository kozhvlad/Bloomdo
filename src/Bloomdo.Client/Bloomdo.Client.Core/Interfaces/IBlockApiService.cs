using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Client.Core.Interfaces;

public interface IBlockApiService
{
    Task<List<BlockRuleResponse>?> GetBlockRulesAsync(CancellationToken ct = default);
    Task<BlockRuleResponse?> CreateBlockRuleAsync(CreateBlockRuleRequest request, CancellationToken ct = default);
    Task<BlockRuleResponse?> UpdateBlockRuleAsync(Guid id, UpdateBlockRuleRequest request, CancellationToken ct = default);
    Task<bool> DeleteBlockRuleAsync(Guid id, CancellationToken ct = default);
}
