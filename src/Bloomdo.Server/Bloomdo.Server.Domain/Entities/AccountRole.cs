namespace Bloomdo.Server.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between <see cref="Account"/> and <see cref="Role"/>.
/// </summary>
public class AccountRole : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
