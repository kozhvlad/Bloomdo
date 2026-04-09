using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.DTOs.Profile;

namespace Bloomdo.Server.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(Guid accountId, string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<AccountProfileResponse> GetProfileAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AccountProfileResponse> UpdateProfileAsync(Guid accountId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfileStatsResponse> GetProfileStatsAsync(Guid accountId, CancellationToken cancellationToken = default);
}

