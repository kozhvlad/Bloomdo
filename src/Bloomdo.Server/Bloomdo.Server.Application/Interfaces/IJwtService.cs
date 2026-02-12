using System.Security.Claims;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid accountId, string email, IReadOnlyList<UserRole> roles, IReadOnlyList<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Guid? GetAccountIdFromToken(string token);
    int AccessTokenExpirationMinutes { get; }
}
