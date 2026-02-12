using Bloomdo.Client.Domain.Enums;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Provides authorization services for the client application.
/// Roles are checked by exact match. Use permissions for capability-based access control.
/// Accounts can have multiple roles simultaneously.
/// </summary>
public interface IAuthorizationService
{
    bool IsAuthorized(Type viewModelType);

    /// <summary>
    /// Checks if the current user has the specified role (exact match).
    /// </summary>
    bool HasRole(UserRole role);

    /// <summary>
    /// Checks if the current user has at least one of the specified roles (exact match).
    /// </summary>
    bool HasAnyRole(params UserRole[] roles);

    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    bool MeetsPolicy(AuthorizationPolicy policy);
    AuthorizationResult CheckAccess(Type viewModelType);
}
