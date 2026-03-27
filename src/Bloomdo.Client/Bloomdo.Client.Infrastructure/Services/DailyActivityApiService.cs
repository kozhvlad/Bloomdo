using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.Infrastructure.Services;

public class DailyActivityApiService(HttpClient httpClient) : IDailyActivityApiService
{
    public async Task<DailyActivitiesResponse?> GetDailyAsync(DateOnly? date = null, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Activities.Daily;
            if (date.HasValue)
                url += $"?date={date.Value:yyyy-MM-dd}";

            var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<DailyActivitiesResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetDaily failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ActivityGroupResponse>?> GetGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(ApiRoutes.Activities.Groups, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ActivityGroupResponse>>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetGroups failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ActivityGroupResponse?> CreateGroupAsync(CreateActivityGroupRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Activities.Groups, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ActivityGroupResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateGroup failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ActivityGroupResponse?> UpdateGroupAsync(Guid id, UpdateActivityGroupRequest request, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/activities/groups/{id}";
            var response = await httpClient.PutAsJsonAsync(url, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ActivityGroupResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateGroup failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/activities/groups/{id}";
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteGroup failed: {ex.Message}");
            return false;
        }
    }

    public async Task<ActivityItemResponse?> CreateItemAsync(CreateActivityItemRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Activities.Items, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ActivityItemResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateItem failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ActivityItemResponse?> UpdateItemAsync(Guid id, UpdateActivityItemRequest request, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/activities/items/{id}";
            var response = await httpClient.PutAsJsonAsync(url, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ActivityItemResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateItem failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/activities/items/{id}";
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteItem failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ToggleCompletionAsync(ToggleCompletionRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Activities.Toggle, request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ToggleCompletion failed: {ex.Message}");
            return false;
        }
    }

    public async Task<VerifyPhotoResponse?> VerifyPhotoAsync(VerifyPhotoRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Activities.VerifyPhoto, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<VerifyPhotoResponse>(ct);

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VerifyPhoto failed: {ex.Message}");
            return null;
        }
    }
}
