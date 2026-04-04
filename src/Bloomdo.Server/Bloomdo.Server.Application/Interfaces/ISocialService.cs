using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;

namespace Bloomdo.Server.Application.Interfaces;

public interface ISocialService
{
    // Search
    Task<List<FollowStatusDto>> SearchUsersAsync(Guid currentAccountId, string query, CancellationToken ct = default);

    // Followers / Following
    Task<List<FollowStatusDto>> GetFollowersAsync(Guid accountId, CancellationToken ct = default);
    Task<List<FollowStatusDto>> GetFollowingAsync(Guid accountId, CancellationToken ct = default);
    Task<List<ProfileSummaryDto>> GetMutualFollowersAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> FollowUserAsync(Guid followerId, Guid targetId, CancellationToken ct = default);
    Task<bool> UnfollowUserAsync(Guid followerId, Guid targetId, CancellationToken ct = default);

    // Follow requests (private profiles)
    Task<List<FollowStatusDto>> GetPendingFollowRequestsAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> RespondToFollowRequestAsync(Guid accountId, Guid followshipId, bool accept, CancellationToken ct = default);

    // Notifications
    Task<List<NotificationDto>> GetNotificationsAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> MarkNotificationReadAsync(Guid accountId, Guid notificationId, CancellationToken ct = default);

    // Shared groups
    Task<List<SharedGroupDto>> GetMySharedGroupsAsync(Guid accountId, CancellationToken ct = default);
    Task<SharedGroupDetailDto?> GetSharedGroupDetailAsync(Guid accountId, Guid groupId, CancellationToken ct = default);
    Task<SharedGroupDto?> CreateSharedGroupAsync(Guid accountId, string title, string icon, string color, CancellationToken ct = default);
    Task<SharedGroupDto?> UpdateSharedGroupAsync(Guid accountId, Guid groupId, UpdateSharedGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteSharedGroupAsync(Guid accountId, Guid groupId, CancellationToken ct = default);
    Task<bool> InviteToGroupAsync(Guid adminId, Guid groupId, Guid inviteeId, CancellationToken ct = default);
    Task<bool> RespondToGroupInviteAsync(Guid accountId, Guid groupId, bool accept, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid adminId, Guid groupId, Guid memberId, CancellationToken ct = default);
}
