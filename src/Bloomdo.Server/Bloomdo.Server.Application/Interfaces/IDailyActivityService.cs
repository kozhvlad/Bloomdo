using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Server.Application.Interfaces;

public interface IDailyActivityService
{
    Task<List<ActivityGroupResponse>> GetGroupsAsync(Guid accountId, CancellationToken ct = default);
    Task<ActivityGroupResponse> CreateGroupAsync(Guid accountId, CreateActivityGroupRequest request, CancellationToken ct = default);
    Task<ActivityGroupResponse?> UpdateGroupAsync(Guid accountId, Guid groupId, UpdateActivityGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteGroupAsync(Guid accountId, Guid groupId, CancellationToken ct = default);

    Task<ActivityItemResponse> CreateItemAsync(Guid accountId, CreateActivityItemRequest request, CancellationToken ct = default);
    Task<ActivityItemResponse?> UpdateItemAsync(Guid accountId, Guid itemId, UpdateActivityItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid accountId, Guid itemId, CancellationToken ct = default);

    Task<DailyActivitiesResponse> GetDailyAsync(Guid accountId, DateOnly date, CancellationToken ct = default);
    Task<bool> ToggleCompletionAsync(Guid accountId, ToggleCompletionRequest request, CancellationToken ct = default);
    Task<VerifyPhotoResponse> VerifyPhotoAsync(Guid accountId, VerifyPhotoRequest request, CancellationToken ct = default);
}
