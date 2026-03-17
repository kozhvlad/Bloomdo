using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.DTOs.Profile;

namespace Bloomdo.Client.Infrastructure.Services;

public class ProfileApiService : IProfileApiService
{
    private readonly HttpClient _httpClient;

    public ProfileApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AccountProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiRoutes.Auth.Me, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccountProfileResponse>(cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get profile failed: {ex.Message}");
            return null;
        }
    }

    public async Task<AccountProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(ApiRoutes.Profile.Update, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccountProfileResponse>(cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update profile failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ProfileStatsResponse?> GetProfileStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiRoutes.Profile.Stats, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProfileStatsResponse>(cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get profile stats failed: {ex.Message}");
            return null;
        }
    }
}
