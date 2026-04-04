using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;

namespace Bloomdo.Client.Infrastructure.Services;

public class SocialApiService(HttpClient httpClient) : ISocialApiService
{
    public async Task<List<FollowStatusDto>> SearchUsersAsync(string query, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<FollowStatusDto>>(
                $"{ApiRoutes.Social.Search}?query={Uri.EscapeDataString(query)}", ct) ?? [];
        }
        catch { return []; }
    }

    public async Task<List<FollowStatusDto>> GetFollowersAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<FollowStatusDto>>(ApiRoutes.Social.Followers, ct) ?? []; }
        catch { return []; }
    }

    public async Task<List<FollowStatusDto>> GetFollowingAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<FollowStatusDto>>(ApiRoutes.Social.Following, ct) ?? []; }
        catch { return []; }
    }

    public async Task<List<ProfileSummaryDto>> GetMutualFollowersAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<ProfileSummaryDto>>(ApiRoutes.Social.MutualFollowers, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> FollowUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.Follow.Replace("{userId}", userId.ToString());
            var response = await httpClient.PostAsync(url, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> UnfollowUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.Unfollow.Replace("{userId}", userId.ToString());
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<FollowStatusDto>> GetFollowRequestsAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<FollowStatusDto>>(ApiRoutes.Social.FollowRequests, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> RespondToFollowRequestAsync(Guid followshipId, bool accept, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.RespondFollowRequest.Replace("{id}", followshipId.ToString()) + $"?accept={accept}";
            var response = await httpClient.PutAsync(url, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<NotificationDto>>(ApiRoutes.Social.Notifications, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.ReadNotification.Replace("{id}", notificationId.ToString());
            var response = await httpClient.PutAsync(url, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<SharedGroupDto>> GetSharedGroupsAsync(CancellationToken ct = default)
    {
        try { return await httpClient.GetFromJsonAsync<List<SharedGroupDto>>(ApiRoutes.Social.SharedGroups, ct) ?? []; }
        catch { return []; }
    }

    public async Task<SharedGroupDetailDto?> GetSharedGroupDetailAsync(Guid groupId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupById.Replace("{id}", groupId.ToString());
            return await httpClient.GetFromJsonAsync<SharedGroupDetailDto>(url, ct);
        }
        catch { return null; }
    }

    public async Task<SharedGroupDto?> CreateSharedGroupAsync(string title, string icon, string color, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiRoutes.Social.SharedGroups}?title={Uri.EscapeDataString(title)}&icon={Uri.EscapeDataString(icon)}&color={Uri.EscapeDataString(color)}";
            var response = await httpClient.PostAsync(url, null, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<SharedGroupDto>(ct);
        }
        catch { return null; }
    }

    public async Task<bool> DeleteSharedGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupById.Replace("{id}", groupId.ToString());
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<SharedGroupDto?> UpdateSharedGroupAsync(Guid groupId, UpdateSharedGroupRequest request, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupUpdate.Replace("{id}", groupId.ToString());
            var response = await httpClient.PutAsJsonAsync(url, request, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<SharedGroupDto>(ct);
        }
        catch { return null; }
    }

    public async Task<bool> InviteToGroupAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupInvite.Replace("{id}", groupId.ToString()) + $"?userId={userId}";
            var response = await httpClient.PostAsync(url, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> RespondToGroupInviteAsync(Guid groupId, bool accept, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupInviteRespond.Replace("{id}", groupId.ToString()) + $"?accept={accept}";
            var response = await httpClient.PutAsync(url, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Social.SharedGroupMemberRemove
                .Replace("{id}", groupId.ToString())
                .Replace("{memberId}", memberId.ToString());
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
