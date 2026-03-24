using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet(ApiRoutes.Chat.Conversations)]
    [RequirePermission(Permissions.ChatAccess)]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var conversations = await chatService.GetConversationsAsync(accountId.Value, ct);
        return Ok(conversations);
    }

    [HttpGet(ApiRoutes.Chat.ConversationById)]
    [RequirePermission(Permissions.ChatAccess)]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var conversation = await chatService.GetConversationAsync(id, accountId.Value, ct);
        return conversation is not null ? Ok(conversation) : NotFound();
    }

    [HttpPost(ApiRoutes.Chat.Conversations)]
    [RequirePermission(Permissions.ChatAccess)]
    public async Task<IActionResult> CreateConversationAndSendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        try
        {
            var response = await chatService.SendMessageAsync(accountId.Value, null, request.Message, ct);
            return Ok(response);
        }
        catch (ChatLimitExceededException ex)
        {
            return StatusCode(429, new { error = ex.Message, maxMessages = ex.MaxMessages });
        }
    }

    [HttpPost(ApiRoutes.Chat.Messages)]
    [RequirePermission(Permissions.ChatAccess)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        try
        {
            var response = await chatService.SendMessageAsync(accountId.Value, id, request.Message, ct);
            return Ok(response);
        }
        catch (ChatLimitExceededException ex)
        {
            return StatusCode(429, new { error = ex.Message, maxMessages = ex.MaxMessages });
        }
    }

    [HttpDelete(ApiRoutes.Chat.ConversationById)]
    [RequirePermission(Permissions.ChatAccess)]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var success = await chatService.DeleteConversationAsync(id, accountId.Value, ct);
        return success ? NoContent() : NotFound();
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
