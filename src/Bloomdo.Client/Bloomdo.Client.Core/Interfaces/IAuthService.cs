using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Core.Interfaces;

public interface IAccessTokenManager
{
    string? AuthToken { get; }
    bool IsAuthenticated { get; }
    AccountProfileResponse? CurrentUser { get; }
    IReadOnlyList<UserRole> CurrentRoles { get; }
    IReadOnlyList<string> CurrentPermissions { get; }

    /// <summary>
    /// Raised when an active session becomes invalid (e.g. refresh token rejected by server).
    /// Subscribers should navigate the user to the login screen.
    /// </summary>
    event Action? SessionInvalidated;

    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email, string password, string username, string? firstName, string? lastName);
    Task<bool> RefreshTokenAsync();
    Task LogoutAsync();
    Task InitializeAsync();

    bool HasRole(UserRole role);
    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    void UpdateCurrentUser(AccountProfileResponse profile);
}
