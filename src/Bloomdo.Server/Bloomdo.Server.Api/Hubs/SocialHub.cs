using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Hubs;

[Authorize]
public class SocialHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var accountId = GetAccountId();
        if (accountId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, accountId.Value.ToString());

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var accountId = GetAccountId();
        if (accountId.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, accountId.Value.ToString());

        await base.OnDisconnectedAsync(exception);
    }

    // Client calls this to subscribe to real-time updates for a specific group
    public async Task JoinGroup(string groupId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");

    public async Task LeaveGroup(string groupId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");

    private Guid? GetAccountId()
    {
        var claim = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
