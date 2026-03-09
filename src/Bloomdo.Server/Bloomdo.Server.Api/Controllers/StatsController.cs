using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class StatsController(IStatsService statsService, IAchievementService achievementService) : ControllerBase
{
    [HttpPost(ApiRoutes.Stats.Sync)]
    [RequirePermission(Permissions.StatsView)]
    public async Task<IActionResult> SyncUsage([FromBody] SyncUsageRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        await statsService.SyncUsageAsync(accountId.Value, request, ct);
        await achievementService.EvaluateAchievementsAsync(accountId.Value, ct);

        return Ok();
    }

    [HttpGet(ApiRoutes.Stats.Daily)]
    [RequirePermission(Permissions.StatsView)]
    public async Task<IActionResult> GetDailyStats([FromQuery] DateOnly date, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var stats = await statsService.GetDailyStatsAsync(accountId.Value, date, ct);
        return stats is not null ? Ok(stats) : NotFound();
    }

    [HttpGet(ApiRoutes.Stats.Calendar)]
    [RequirePermission(Permissions.StatsView)]
    public async Task<IActionResult> GetMonthCalendar([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var calendar = await statsService.GetMonthCalendarAsync(accountId.Value, year, month, ct);
        return Ok(calendar);
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
