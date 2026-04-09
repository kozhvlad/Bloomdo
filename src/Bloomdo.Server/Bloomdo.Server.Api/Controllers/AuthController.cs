using Bloomdo.Server.Api.Authorization;
using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.Constants;
using Bloomdo.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

/// <summary>
/// Handles authentication and token lifecycle operations.
/// Domain exceptions are mapped to HTTP status codes by <see cref="Middleware.ExceptionHandlingMiddleware"/>.
/// </summary>
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(ApiRoutes.Auth.Register)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, GetIpAddress(), cancellationToken);
        return Ok(response);
    }

    [HttpPost(ApiRoutes.Auth.Login)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, GetIpAddress(), cancellationToken);

        if (response == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(response);
    }

    [HttpPost(ApiRoutes.Auth.Refresh)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken, GetIpAddress(), cancellationToken);

        if (response == null)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        return Ok(response);
    }

    [Authorize]
    [HttpPost(ApiRoutes.Auth.Revoke)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized(new { message = "Invalid token" });

        await _authService.RevokeTokenAsync(accountId, request.RefreshToken, GetIpAddress(), cancellationToken);
        return Ok(new { message = "Token revoked" });
    }

    [Authorize]
    [RequirePermission(Permissions.ProfileView)]
    [HttpGet(ApiRoutes.Auth.Me)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var accountIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(accountIdClaim, out var accountId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var profile = await _authService.GetProfileAsync(accountId, cancellationToken);
        return Ok(profile);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            // Take only the leftmost (client) IP; the rest are proxies
            var raw = forwardedFor.ToString();
            var firstIp = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private bool TryGetAccountId(out Guid accountId)
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out accountId);
    }

    [Authorize]
    [RequirePermission(Permissions.ProfileEdit)]
    [HttpPut(ApiRoutes.Profile.Update)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized(new { message = "Invalid token" });

        var profile = await _authService.UpdateProfileAsync(accountId, request, cancellationToken);
        return Ok(profile);
    }

    [Authorize]
    [RequirePermission(Permissions.ProfileView)]
    [HttpGet(ApiRoutes.Profile.Stats)]
    public async Task<IActionResult> GetProfileStats(CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId))
            return Unauthorized(new { message = "Invalid token" });

        var stats = await _authService.GetProfileStatsAsync(accountId, cancellationToken);
        return Ok(stats);
    }
}