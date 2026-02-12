using System.Reflection;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Attributes;
using Bloomdo.Client.Domain.Enums;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IAccessTokenManager _accessTokenManager;

    public AuthorizationService(IAccessTokenManager accessTokenManager)
    {
        _accessTokenManager = accessTokenManager;
    }

    public bool IsAuthorized(Type viewModelType)
    {
        return CheckAccess(viewModelType).IsAuthorized;
    }

    /// <summary>
    /// Checks if the current user has the specified role (exact match).
    /// No hierarchy — use permissions for capability-based checks.
    /// </summary>
    public bool HasRole(UserRole role)
    {
        if (!_accessTokenManager.IsAuthenticated)
            return false;

        return _accessTokenManager.HasRole(role);
    }

    /// <summary>
    /// Checks if the current user has at least one of the specified roles (exact match).
    /// </summary>
    public bool HasAnyRole(params UserRole[] roles)
    {
        return roles.Any(HasRole);
    }

    public bool HasPermission(string permission)
    {
        return _accessTokenManager.HasPermission(permission);
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        return _accessTokenManager.HasAnyPermission(permissions);
    }

    public bool MeetsPolicy(AuthorizationPolicy policy)
    {
        return policy switch
        {
            AuthorizationPolicy.None => true,
            AuthorizationPolicy.RequireAuthentication => _accessTokenManager.IsAuthenticated,
            AuthorizationPolicy.RequirePremium => HasPermission(Permissions.PremiumAccess),
            AuthorizationPolicy.RequireModerator => HasPermission(Permissions.UsersManage),
            AuthorizationPolicy.RequireAdmin => HasPermission(Permissions.RolesManage),
            _ => false
        };
    }

    public AuthorizationResult CheckAccess(Type viewModelType)
    {
        var authorizeAttribute = viewModelType.GetCustomAttribute<AuthorizeAttribute>();

        if (authorizeAttribute == null)
            return AuthorizationResult.Success();

        if (!_accessTokenManager.IsAuthenticated)
        {
            return AuthorizationResult.Failure(
                "Authentication is required to access this page",
                AuthorizationFailureType.NotAuthenticated);
        }

        if (authorizeAttribute.Policy != AuthorizationPolicy.None)
        {
            if (!MeetsPolicy(authorizeAttribute.Policy))
            {
                return AuthorizationResult.Failure(
                    $"Insufficient privileges: policy {authorizeAttribute.Policy} required",
                    AuthorizationFailureType.PolicyNotMet);
            }
        }

        if (authorizeAttribute.Roles.Length > 0)
        {
            if (!HasAnyRole(authorizeAttribute.Roles))
            {
                var requiredRoles = string.Join(", ", authorizeAttribute.Roles);
                return AuthorizationResult.Failure(
                    $"One of the following roles is required: {requiredRoles}",
                    AuthorizationFailureType.InsufficientRole);
            }
        }

        if (authorizeAttribute.Permissions.Length > 0)
        {
            if (!HasAnyPermission(authorizeAttribute.Permissions))
            {
                var requiredPermissions = string.Join(", ", authorizeAttribute.Permissions);
                return AuthorizationResult.Failure(
                    $"One of the following permissions is required: {requiredPermissions}",
                    AuthorizationFailureType.InsufficientPermission);
            }
        }

        return AuthorizationResult.Success();
    }
}
