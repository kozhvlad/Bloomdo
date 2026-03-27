using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.Core.Interfaces;

public interface IDailyActivityApiService
{
    Task<DailyActivitiesResponse?> GetDailyAsync(DateOnly? date = null, CancellationToken ct = default);
    Task<List<ActivityGroupResponse>?> GetGroupsAsync(CancellationToken ct = default);
    Task<ActivityGroupResponse?> CreateGroupAsync(CreateActivityGroupRequest request, CancellationToken ct = default);
    Task<ActivityGroupResponse?> UpdateGroupAsync(Guid id, UpdateActivityGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteGroupAsync(Guid id, CancellationToken ct = default);
    Task<ActivityItemResponse?> CreateItemAsync(CreateActivityItemRequest request, CancellationToken ct = default);
    Task<ActivityItemResponse?> UpdateItemAsync(Guid id, UpdateActivityItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid id, CancellationToken ct = default);
    Task<bool> ToggleCompletionAsync(ToggleCompletionRequest request, CancellationToken ct = default);
    Task<VerifyPhotoResponse?> VerifyPhotoAsync(VerifyPhotoRequest request, CancellationToken ct = default);
}
