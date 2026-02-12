using Bloomdo.Client.Domain.Enums;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Domain.Attributes;

/// <summary>
/// Marks a ViewModel as requiring authorization before navigation.
/// Supports role-based (exact match), permission-based, and policy-based access control.
/// 
/// <para>
/// <b>Roles</b> are checked by exact match — no hierarchy.
/// Use <b>Permissions</b> for capability-based checks (e.g. "premium:access").
/// </para>
/// 
/// <example>
/// <code>
/// // Requires the Premium role explicitly
/// [Authorize(UserRole.Premium)]
/// 
/// // Requires authentication only
/// [Authorize(AuthorizationPolicy.RequireAuthentication)]
/// 
/// // Requires specific permission (preferred for capability checks)
/// [Authorize] { Permissions = [Permissions.UsersManage] }
/// </code>
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Required roles (exact match). The user must have at least one of these roles assigned.
    /// </summary>
    public UserRole[] Roles { get; init; }

    /// <summary>
    /// Required permissions. User must have at least one of the specified permissions.
    /// </summary>
    public string[] Permissions { get; init; }

    /// <summary>
    /// Authorization policy to enforce.
    /// </summary>
    public AuthorizationPolicy Policy { get; init; }

    public AuthorizeAttribute(AuthorizationPolicy policy = AuthorizationPolicy.RequireAuthentication)
    {
        Policy = policy;
        Roles = [];
        Permissions = [];
    }

    public AuthorizeAttribute(params UserRole[] roles)
    {
        Roles = roles;
        Permissions = [];
        Policy = AuthorizationPolicy.None;
    }
}
