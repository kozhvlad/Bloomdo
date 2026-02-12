using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AppDbContext _context;

    public RolePermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<UserRole> roles, CancellationToken cancellationToken = default)
    {
        var roleIds = roles.Select(r => (int)r).ToList();
        return await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == (int)role)
            .ToListAsync(cancellationToken);

        _context.RolePermissions.RemoveRange(existing);

        var newEntries = permissions.Select(p => new RolePermission
        {
            RoleId = (int)role,
            Permission = p
        });

        await _context.RolePermissions.AddRangeAsync(newEntries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<UserRole, IReadOnlyList<string>>> GetAllRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.RolePermissions
            .ToListAsync(cancellationToken);

        return all
            .GroupBy(rp => (UserRole)rp.RoleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(rp => rp.Permission).ToList());
    }
}
