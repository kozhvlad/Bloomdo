using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class SocialController(ISocialService socialService) : ControllerBase
{
    // ─── Search ───────────────────────────────────────────────────────────────

    [HttpGet(ApiRoutes.Social.Search)]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.SearchUsersAsync(accountId.Value, query, ct);
        return Ok(result);
    }

    // ─── Followers / Following ────────────────────────────────────────────────

    [HttpGet(ApiRoutes.Social.Followers)]
    public async Task<IActionResult> GetFollowers(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetFollowersAsync(accountId.Value, ct));
    }

    [HttpGet(ApiRoutes.Social.Following)]
    public async Task<IActionResult> GetFollowing(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetFollowingAsync(accountId.Value, ct));
    }

    [HttpGet(ApiRoutes.Social.MutualFollowers)]
    public async Task<IActionResult> GetMutual(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetMutualFollowersAsync(accountId.Value, ct));
    }

    [HttpPost(ApiRoutes.Social.Follow)]
    public async Task<IActionResult> Follow(Guid userId, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.FollowUserAsync(accountId.Value, userId, ct);
        return result ? Ok() : BadRequest("Already following or user not found.");
    }

    [HttpDelete(ApiRoutes.Social.Unfollow)]
    public async Task<IActionResult> Unfollow(Guid userId, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.UnfollowUserAsync(accountId.Value, userId, ct);
        return result ? NoContent() : NotFound();
    }

    // ─── Follow Requests ──────────────────────────────────────────────────────

    [HttpGet(ApiRoutes.Social.FollowRequests)]
    public async Task<IActionResult> GetFollowRequests(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetPendingFollowRequestsAsync(accountId.Value, ct));
    }

    [HttpPut(ApiRoutes.Social.RespondFollowRequest)]
    public async Task<IActionResult> RespondFollowRequest(Guid id, [FromQuery] bool accept, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.RespondToFollowRequestAsync(accountId.Value, id, accept, ct);
        return result ? Ok() : BadRequest("Request not found or unauthorized.");
    }

    // ─── Notifications ────────────────────────────────────────────────────────

    [HttpGet(ApiRoutes.Social.Notifications)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetNotificationsAsync(accountId.Value, ct));
    }

    [HttpPut(ApiRoutes.Social.ReadNotification)]
    public async Task<IActionResult> ReadNotification(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.MarkNotificationReadAsync(accountId.Value, id, ct);
        return result ? Ok() : NotFound();
    }

    // ─── Shared Groups ────────────────────────────────────────────────────────

    [HttpGet(ApiRoutes.Social.SharedGroups)]
    public async Task<IActionResult> GetSharedGroups(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        return Ok(await socialService.GetMySharedGroupsAsync(accountId.Value, ct));
    }

    [HttpPost(ApiRoutes.Social.SharedGroups)]
    public async Task<IActionResult> CreateSharedGroup(
        [FromQuery] string title,
        [FromQuery] string icon = "📋",
        [FromQuery] string color = "#7E57C2",
        CancellationToken ct = default)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var group = await socialService.CreateSharedGroupAsync(accountId.Value, title, icon, color, ct);
        return group is null ? BadRequest() : Ok(group);
    }

    [HttpGet(ApiRoutes.Social.SharedGroupById)]
    public async Task<IActionResult> GetSharedGroup(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var detail = await socialService.GetSharedGroupDetailAsync(accountId.Value, id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPut(ApiRoutes.Social.SharedGroupUpdate)]
    public async Task<IActionResult> UpdateSharedGroup(Guid id, [FromBody] UpdateSharedGroupRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.UpdateSharedGroupAsync(accountId.Value, id, request, ct);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpDelete(ApiRoutes.Social.SharedGroupById)]
    public async Task<IActionResult> DeleteSharedGroup(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.DeleteSharedGroupAsync(accountId.Value, id, ct);
        return result ? NoContent() : Forbid();
    }

    [HttpPost(ApiRoutes.Social.SharedGroupInvite)]
    public async Task<IActionResult> InviteToGroup(Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.InviteToGroupAsync(accountId.Value, id, userId, ct);
        return result ? Ok() : BadRequest("Cannot invite: not admin, not mutual follower, or already invited.");
    }

    [HttpPut(ApiRoutes.Social.SharedGroupInviteRespond)]
    public async Task<IActionResult> RespondGroupInvite(Guid id, [FromQuery] bool accept, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.RespondToGroupInviteAsync(accountId.Value, id, accept, ct);
        return result ? Ok() : BadRequest("Invite not found.");
    }

    [HttpDelete(ApiRoutes.Social.SharedGroupMemberRemove)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.RemoveMemberAsync(accountId.Value, id, memberId, ct);
        return result ? NoContent() : Forbid();
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
