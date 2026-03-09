using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Server.Application.Interfaces;

public interface IBlockService
{
    Task<List<BlockRuleResponse>> GetBlockRulesAsync(Guid accountId, CancellationToken ct = default);
    Task<BlockRuleResponse> CreateBlockRuleAsync(Guid accountId, CreateBlockRuleRequest request, CancellationToken ct = default);
    Task<BlockRuleResponse?> UpdateBlockRuleAsync(Guid accountId, Guid ruleId, UpdateBlockRuleRequest request, CancellationToken ct = default);
    Task<bool> DeleteBlockRuleAsync(Guid accountId, Guid ruleId, CancellationToken ct = default);
}
