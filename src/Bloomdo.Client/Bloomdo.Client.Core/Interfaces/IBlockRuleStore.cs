using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Client.Core.Interfaces;

public interface IBlockRuleStore
{
    Task SaveRulesAsync(IReadOnlyList<BlockRuleResponse> rules);
    Task<IReadOnlyList<BlockRuleResponse>> LoadRulesAsync();
}
