using Bloomdo.Shared.DTOs.Auth;
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

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken,
            cancellationToken);

        if (token == null || !token.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var account = await _accountRepository.GetByIdAsync(token.AccountId, cancellationToken);
        if (account == null)
        {
            throw new AccountNotFoundException(token.AccountId);
        }

        // Rotate the refresh token
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        var roles = GetAccountRoles(account);
        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(roles, cancellationToken);

        var newToken = new RefreshToken
        {
            AccountId = account.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };
        await _refreshTokenRepository.AddAsync(newToken, cancellationToken);

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
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            IsEmailConfirmed = account.IsEmailConfirmed,
            LastLoginAt = account.LastLoginAt,
            CreatedAt = account.CreatedAt
        };
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
}
