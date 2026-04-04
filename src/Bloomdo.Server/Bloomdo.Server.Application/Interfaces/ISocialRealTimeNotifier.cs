using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;

namespace Bloomdo.Server.Application.Interfaces;

public interface ISocialRealTimeNotifier
{
    Task SendNewFollowerAsync(Guid recipientId, ProfileSummaryDto follower, CancellationToken ct = default);
    Task SendGroupInviteAsync(Guid recipientId, SharedGroupDto group, ProfileSummaryDto inviter, CancellationToken ct = default);
    Task SendGroupDeletedAsync(Guid recipientId, Guid groupId, CancellationToken ct = default);
    Task SendNewGroupMemberAsync(Guid recipientId, Guid groupId, ProfileSummaryDto member, CancellationToken ct = default);
    Task SendTaskCompletedAsync(Guid groupId, Guid actorId, Guid itemId, CancellationToken ct = default);
    Task SendNewGroupTaskAsync(Guid groupId, Guid itemId, CancellationToken ct = default);
}
