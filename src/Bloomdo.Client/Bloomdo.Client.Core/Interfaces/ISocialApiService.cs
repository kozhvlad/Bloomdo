using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;

namespace Bloomdo.Client.Core.Interfaces;

public interface ISocialApiService
{
    // Search
    Task<List<FollowStatusDto>> SearchUsersAsync(string query, CancellationToken ct = default);

    // Followers / Following
    Task<List<FollowStatusDto>> GetFollowersAsync(CancellationToken ct = default);
    Task<List<FollowStatusDto>> GetFollowingAsync(CancellationToken ct = default);
    Task<List<ProfileSummaryDto>> GetMutualFollowersAsync(CancellationToken ct = default);
    Task<bool> FollowUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> UnfollowUserAsync(Guid userId, CancellationToken ct = default);

    // Follow requests
    Task<List<FollowStatusDto>> GetFollowRequestsAsync(CancellationToken ct = default);
    Task<bool> RespondToFollowRequestAsync(Guid followshipId, bool accept, CancellationToken ct = default);

    // Notifications
    Task<List<NotificationDto>> GetNotificationsAsync(CancellationToken ct = default);
    Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct = default);

    // Shared groups
    Task<List<SharedGroupDto>> GetSharedGroupsAsync(CancellationToken ct = default);
    Task<SharedGroupDetailDto?> GetSharedGroupDetailAsync(Guid groupId, CancellationToken ct = default);
    Task<SharedGroupDto?> CreateSharedGroupAsync(string title, string icon, string color, CancellationToken ct = default);
    Task<SharedGroupDto?> UpdateSharedGroupAsync(Guid groupId, UpdateSharedGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteSharedGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<bool> InviteToGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<bool> RespondToGroupInviteAsync(Guid groupId, bool accept, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct = default);
}
