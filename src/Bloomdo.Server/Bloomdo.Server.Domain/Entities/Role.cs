namespace Bloomdo.Server.Domain.Entities;

/// <summary>
/// Lookup table for application roles.
/// Primary key values match the <see cref="Bloomdo.Shared.Enums.UserRole"/> enum.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<AccountRole> AccountRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
