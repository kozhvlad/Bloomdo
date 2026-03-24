using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Blocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class BlocksController(IBlockService blockService) : ControllerBase
{
    [HttpGet(ApiRoutes.Blocks.List)]
    [RequirePermission(Permissions.BlocksManage)]
    public async Task<IActionResult> GetBlockRules(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var rules = await blockService.GetBlockRulesAsync(accountId.Value, ct);
        return Ok(rules);
    }

    [HttpPost(ApiRoutes.Blocks.Create)]
    [RequirePermission(Permissions.BlocksManage)]
    public async Task<IActionResult> CreateBlockRule([FromBody] CreateBlockRuleRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        try
        {
            var rule = await blockService.CreateBlockRuleAsync(accountId.Value, request, ct);
            return Created(string.Empty, rule);
        }
        catch (BlockLimitExceededException ex)
        {
            return StatusCode(403, new { error = ex.Message, maxBlocks = ex.MaxBlocks });
        }
    }

    [HttpPut("api/blocks/{id:guid}")]
    [RequirePermission(Permissions.BlocksManage)]
    public async Task<IActionResult> UpdateBlockRule(Guid id, [FromBody] UpdateBlockRuleRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var rule = await blockService.UpdateBlockRuleAsync(accountId.Value, id, request, ct);
        return rule is not null ? Ok(rule) : NotFound();
    }

    [HttpDelete("api/blocks/{id:guid}")]
    [RequirePermission(Permissions.BlocksManage)]
    public async Task<IActionResult> DeleteBlockRule(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var deleted = await blockService.DeleteBlockRuleAsync(accountId.Value, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
