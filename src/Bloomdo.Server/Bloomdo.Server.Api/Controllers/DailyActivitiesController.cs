using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class DailyActivitiesController(IDailyActivityService activityService) : ControllerBase
{
    [HttpGet(ApiRoutes.Activities.Daily)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> GetDaily([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await activityService.GetDailyAsync(accountId.Value, targetDate, ct);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Activities.Groups)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> GetGroups(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var groups = await activityService.GetGroupsAsync(accountId.Value, ct);
        return Ok(groups);
    }

    [HttpPost(ApiRoutes.Activities.Groups)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateActivityGroupRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var group = await activityService.CreateGroupAsync(accountId.Value, request, ct);
        return Created(string.Empty, group);
    }

    [HttpPut("api/activities/groups/{id:guid}")]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateActivityGroupRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var group = await activityService.UpdateGroupAsync(accountId.Value, id, request, ct);
        return group is not null ? Ok(group) : NotFound();
    }

    [HttpDelete("api/activities/groups/{id:guid}")]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var deleted = await activityService.DeleteGroupAsync(accountId.Value, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost(ApiRoutes.Activities.Items)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> CreateItem([FromBody] CreateActivityItemRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var item = await activityService.CreateItemAsync(accountId.Value, request, ct);
        return Created(string.Empty, item);
    }

    [HttpPut("api/activities/items/{id:guid}")]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateActivityItemRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var item = await activityService.UpdateItemAsync(accountId.Value, id, request, ct);
        return item is not null ? Ok(item) : NotFound();
    }

    [HttpDelete("api/activities/items/{id:guid}")]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var deleted = await activityService.DeleteItemAsync(accountId.Value, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost(ApiRoutes.Activities.Toggle)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> ToggleCompletion([FromBody] ToggleCompletionRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await activityService.ToggleCompletionAsync(accountId.Value, request, ct);
        return result ? Ok() : NotFound();
    }

    [HttpPost(ApiRoutes.Activities.VerifyPhoto)]
    [RequirePermission(Permissions.ActivitiesManage)]
    public async Task<IActionResult> VerifyPhoto([FromBody] VerifyPhotoRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        try
        {
            var result = await activityService.VerifyPhotoAsync(accountId.Value, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API keys exhausted") || ex.Message.Contains("unavailable"))
        {
            return StatusCode(503, new { message = "Vision service is temporarily unavailable. Please try again later." });
        }
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
