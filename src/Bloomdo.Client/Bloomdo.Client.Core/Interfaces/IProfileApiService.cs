using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.DTOs.Profile;

namespace Bloomdo.Client.Core.Interfaces;

public interface IProfileApiService
{
    Task<AccountProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<AccountProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfileStatsResponse?> GetProfileStatsAsync(CancellationToken cancellationToken = default);
}
