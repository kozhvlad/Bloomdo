using Bloomdo.Server.Api.Hubs;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;
using Microsoft.AspNetCore.SignalR;

namespace Bloomdo.Server.Api.Services;

public class SocialRealTimeNotifier(IHubContext<SocialHub> hub) : ISocialRealTimeNotifier
{
    public Task SendNewFollowerAsync(Guid recipientId, ProfileSummaryDto follower, CancellationToken ct = default)
        => hub.Clients.Group(recipientId.ToString()).SendAsync("ReceiveNewFollower", follower, cancellationToken: ct);

    public Task SendGroupInviteAsync(Guid recipientId, SharedGroupDto group, ProfileSummaryDto inviter, CancellationToken ct = default)
        => hub.Clients.Group(recipientId.ToString()).SendAsync("ReceiveGroupInvite", group, inviter, cancellationToken: ct);

    public Task SendGroupDeletedAsync(Guid recipientId, Guid groupId, CancellationToken ct = default)
        => hub.Clients.Group(recipientId.ToString()).SendAsync("ReceiveGroupDeleted", groupId, cancellationToken: ct);

    public Task SendNewGroupMemberAsync(Guid recipientId, Guid groupId, ProfileSummaryDto member, CancellationToken ct = default)
        => hub.Clients.Group(recipientId.ToString()).SendAsync("ReceiveNewGroupMember", groupId, member, cancellationToken: ct);

    public Task SendTaskCompletedAsync(Guid groupId, Guid actorId, Guid itemId, CancellationToken ct = default)
        => hub.Clients.Group($"group_{groupId}").SendAsync("ReceiveTaskCompleted", actorId, itemId, cancellationToken: ct);

    public Task SendNewGroupTaskAsync(Guid groupId, Guid itemId, CancellationToken ct = default)
        => hub.Clients.Group($"group_{groupId}").SendAsync("ReceiveNewGroupTask", itemId, cancellationToken: ct);
}
