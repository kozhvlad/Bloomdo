using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;

namespace Bloomdo.Client.Core.Interfaces;

public interface ISignalRClientService
{
    bool IsConnected { get; }

    Task ConnectAsync(string token, CancellationToken ct = default);
    Task DisconnectAsync();

    Task JoinGroupAsync(Guid groupId);
    Task LeaveGroupAsync(Guid groupId);

    event Action<ProfileSummaryDto>? NewFollowerReceived;
    event Action<SharedGroupDto, ProfileSummaryDto>? GroupInviteReceived;
    event Action<Guid>? GroupDeletedReceived;
    event Action<Guid, ProfileSummaryDto>? NewGroupMemberReceived;
    event Action<Guid, Guid>? TaskCompletedReceived;   // (actorId, itemId)
    event Action<Guid>? NewGroupTaskReceived;           // (itemId)
}
