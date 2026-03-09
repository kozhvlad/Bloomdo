using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class AchievementsController(IAchievementService achievementService) : ControllerBase
{
    [HttpGet(ApiRoutes.Achievements.List)]
    [RequirePermission(Permissions.StatsView)]
    public async Task<IActionResult> GetAchievements(CancellationToken ct)
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(claim, out var accountId)) return Unauthorized();

        var achievements = await achievementService.GetAchievementsAsync(accountId, ct);
        return Ok(achievements);
    }
}
