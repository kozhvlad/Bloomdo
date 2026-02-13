using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Infrastructure.Services;

public class AccessTokenManager : IAccessTokenManager
{
    private readonly IAuthApiService _authApiService;
    private readonly ITokenStorage _tokenStorage;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _accessTokenExpiresAt;
    private AccountProfileResponse? _currentUser;
    private List<UserRole> _currentRoles = [];
    private List<string> _currentPermissions = [];

    public string? AuthToken => _accessToken;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    public AccountProfileResponse? CurrentUser => _currentUser;
    public IReadOnlyList<UserRole> CurrentRoles => _currentRoles;
    public IReadOnlyList<string> CurrentPermissions => _currentPermissions;

    /// <summary>
    /// Indicates whether the access token will expire within the next 30 seconds.
    /// Used by <see cref="Middleware.AuthHeaderHandler"/> for proactive refresh.
    /// </summary>
    public bool IsAccessTokenExpiringSoon => _accessTokenExpiresAt <= DateTime.UtcNow.AddSeconds(30);

    public AccessTokenManager(IAuthApiService authApiService, ITokenStorage tokenStorage)
    {
        _authApiService = authApiService;
        _tokenStorage = tokenStorage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _authApiService.LoginAsync(request);

            if (response == null)
                return false;

            ApplyAuthResponse(response);
            await SaveTokens(response.AccessToken, response.RefreshToken);
            await LoadProfile();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password, string? firstName, string? lastName)
    {
        try
        {
            var request = new RegisterRequest
            {
                Email = email,
                Password = password,
                FirstName = firstName,
                LastName = lastName
            };

            var response = await _authApiService.RegisterAsync(request);

            if (response == null)
                return false;

            ApplyAuthResponse(response);
            await SaveTokens(response.AccessToken, response.RefreshToken);
            await LoadProfile();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register failed: {ex.Message}");
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _accessToken = await _tokenStorage.GetAccessTokenAsync();
            _refreshToken = await _tokenStorage.GetRefreshTokenAsync();

            if (!string.IsNullOrEmpty(_accessToken))
            {
                ParseJwtClaims(_accessToken);
            }

            if (IsAuthenticated)
            {
                await LoadProfile();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load tokens: {ex.Message}");
            await LogoutAsync();
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
            return false;

        try
        {
            var response = await _authApiService.RefreshTokenAsync(_refreshToken);

            if (response == null)
            {
                await LogoutAsync();
                return false;
            }

            ApplyAuthResponse(response);
            await SaveTokens(response.AccessToken, response.RefreshToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Refresh token failed: {ex.Message}");
            await LogoutAsync();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                await _authApiService.RevokeTokenAsync(_refreshToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Revoke token failed: {ex.Message}");
        }
        finally
        {
            ClearState();
            await _tokenStorage.ClearTokensAsync();
        }
    }

    public bool HasRole(UserRole role)
    {
        return _currentRoles.Contains(role);
    }

    public bool HasPermission(string permission)
    {
        return _currentPermissions.Contains(permission);
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(HasPermission);
    }

    private void ApplyAuthResponse(AuthResponse response)
    {
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _accessTokenExpiresAt = response.AccessTokenExpiresAt;
        _currentRoles = response.Roles;
        _currentPermissions = response.Permissions;
    }

    private async Task SaveTokens(string accessToken, string refreshToken)
    {
        await _tokenStorage.SaveTokensAsync(accessToken, refreshToken);
    }

    private async Task LoadProfile()
    {
        try
        {
            _currentUser = await _authApiService.GetProfileAsync();

            if (_currentUser != null)
            {
                _currentRoles = _currentUser.Roles;
                _currentPermissions = _currentUser.Permissions;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load profile: {ex.Message}");
            _currentUser = null;
        }
    }

    private void ClearState()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiresAt = DateTime.MinValue;
        _currentUser = null;
        _currentRoles = [];
        _currentPermissions = [];
    }

    /// <summary>
    /// Lightweight JWT payload parsing (no signature validation).
    /// Used to extract role, permissions and expiry when restoring tokens from storage.
    /// </summary>
    private void ParseJwtClaims(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return;

            var jwtToken = handler.ReadJwtToken(token);

            // Expiry
            _accessTokenExpiresAt = jwtToken.ValidTo;

            // Roles (ClaimTypes.Role or "role")
            _currentRoles = [];
            var roleClaims = jwtToken.Claims.Where(c => 
                c.Type == ClaimTypes.Role || c.Type == "role");

            foreach (var claim in roleClaims)
            {
                if (Enum.TryParse<UserRole>(claim.Value, true, out var role))
                    _currentRoles.Add(role);
            }

            // Permissions
            _currentPermissions = jwtToken.Claims
                .Where(c => c.Type == AppClaimTypes.Permission)
                .Select(c => c.Value)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse JWT claims: {ex.Message}");
        }
    }
}