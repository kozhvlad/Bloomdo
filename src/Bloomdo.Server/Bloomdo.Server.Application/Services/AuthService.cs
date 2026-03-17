using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.Enums;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Domain.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Settings;

namespace Bloomdo.Server.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IJwtService _jwtService;
    private readonly IAuthSettings _authSettings;

    public AuthService(
        IAccountRepository accountRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IRolePermissionRepository rolePermissionRepository,
        IJwtService jwtService,
        IAuthSettings authSettings)
    {
        _accountRepository = accountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _jwtService = jwtService;
        _authSettings = authSettings;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (await _accountRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new EmailAlreadyExistsException(request.Email);
        }

        var account = new Account
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailConfirmed = false,
            AccountRoles = [new AccountRole { RoleId = (int)UserRole.User }]
        };

        await _accountRepository.AddAsync(account, cancellationToken);

        return await GenerateAuthResponseAsync(account, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        account.LastLoginAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account, cancellationToken);

        return await GenerateAuthResponseAsync(account, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken,
            cancellationToken);

        if (token == null)
        {
            return null;
        }

        // Grace period: if this token was just revoked (< 30s ago) and was replaced,
        // treat as a duplicate/retry request — return the replacement token's response.
        if (!token.IsActive && token.IsRevoked && token.ReplacedByToken != null
            && token.RevokedAt.HasValue && (DateTime.UtcNow - token.RevokedAt.Value).TotalSeconds < 30)
        {
            var replacementToken = await _refreshTokenRepository.FirstOrDefaultAsync(
                rt => rt.Token == token.ReplacedByToken,
                cancellationToken);

            if (replacementToken is { IsActive: true })
            {
                var acct = await _accountRepository.GetByIdAsync(replacementToken.AccountId, cancellationToken)
                           ?? throw new AccountNotFoundException(replacementToken.AccountId);

                var acctRoles = GetAccountRoles(acct);
                var acctPerms = await _rolePermissionRepository.GetPermissionsForRolesAsync(acctRoles, cancellationToken);
                var acctAccessToken = _jwtService.GenerateAccessToken(acct.Id, acct.Email, acctRoles, acctPerms);

                return new AuthResponse
                {
                    Id = acct.Id,
                    Email = acct.Email,
                    FirstName = acct.FirstName,
                    LastName = acct.LastName,
                    Roles = acctRoles.ToList(),
                    Permissions = acctPerms.ToList(),
                    AccessToken = acctAccessToken,
                    RefreshToken = replacementToken.Token,
                    AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.AccessTokenExpirationMinutes)
                };
            }
        }

        if (!token.IsActive)
        {
            return null;
        }

        var account = await _accountRepository.GetByIdAsync(token.AccountId, cancellationToken);
        if (account == null)
        {
            throw new AccountNotFoundException(token.AccountId);
        }

        // Rotate the refresh token
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var roles = GetAccountRoles(account);
        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(roles, cancellationToken);

        var newToken = new RefreshToken
        {
            AccountId = account.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        // Persist new token first, so the user is never locked out
        await _refreshTokenRepository.AddAsync(newToken, cancellationToken);

        // Then revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(account.Id, account.Email, roles, permissions);

        return new AuthResponse
        {
            Id = account.Id,
            Email = account.Email,
            FirstName = account.FirstName,
            LastName = account.LastName,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.AccessTokenExpirationMinutes)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken,
            cancellationToken);

        if (token == null || !token.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
    }

    public async Task<AccountProfileResponse> GetProfileAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            throw new AccountNotFoundException(accountId);
        }

        var roles = GetAccountRoles(account);
        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(roles, cancellationToken);

        return new AccountProfileResponse
        {
            Id = account.Id,
            Email = account.Email,
            FirstName = account.FirstName,
            LastName = account.LastName,
            Username = account.Username,
            Bio = account.Bio,
            Avatar = DeserializeAvatar(account.AvatarJson),
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            IsEmailConfirmed = account.IsEmailConfirmed,
            LastLoginAt = account.LastLoginAt,
            CreatedAt = account.CreatedAt
        };
    }

    public async Task<AccountProfileResponse> UpdateProfileAsync(Guid accountId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        if (request.FirstName != null) account.FirstName = request.FirstName;
        if (request.LastName != null) account.LastName = request.LastName;
        if (request.Username != null) account.Username = request.Username;
        if (request.Bio != null) account.Bio = request.Bio;
        if (request.Avatar != null) account.AvatarJson = System.Text.Json.JsonSerializer.Serialize(request.Avatar);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        return await GetProfileAsync(accountId, cancellationToken);
    }

    public Task<ProfileStatsResponse> GetProfileStatsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        // Placeholder — real values should be computed from domain data
        return Task.FromResult(new ProfileStatsResponse
        {
            StreakDays = 0,
            TasksCompleted = 0,
            FocusHours = 0,
            TotalBlocksCreated = 0,
            AchievementsUnlocked = 0,
            JoinedAt = DateTime.UtcNow
        });
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(Account account, string ipAddress, CancellationToken cancellationToken)
    {
        var roles = GetAccountRoles(account);
        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(roles, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(account.Id, account.Email, roles, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            AccountId = account.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return new AuthResponse
        {
            Id = account.Id,
            Email = account.Email,
            FirstName = account.FirstName,
            LastName = account.LastName,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.AccessTokenExpirationMinutes)
        };
    }

    private static IReadOnlyList<UserRole> GetAccountRoles(Account account)
    {
        return account.AccountRoles
            .Select(ar => (UserRole)ar.RoleId)
            .OrderBy(r => r)
            .ToList();
    }

    private static AvatarConfig? DeserializeAvatar(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AvatarConfig>(json);
        }
        catch
        {
            return null;
        }
    }
}
