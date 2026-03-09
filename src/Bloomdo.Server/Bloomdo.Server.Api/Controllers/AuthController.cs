using Bloomdo.Shared.DTOs.Auth;
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
        await _authService.RevokeTokenAsync(request.RefreshToken, GetIpAddress(), cancellationToken);
        return Ok(new { message = "Token revoked" });
    }

    [Authorize]
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
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}