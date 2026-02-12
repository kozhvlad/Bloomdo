using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<UserRole> roles, CancellationToken cancellationToken = default);
    Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<UserRole, IReadOnlyList<string>>> GetAllRolePermissionsAsync(CancellationToken cancellationToken = default);
}
