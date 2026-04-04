using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }
    public Guid? ActorId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }

    public Account Recipient { get; set; } = null!;
    public Account? Actor { get; set; }
}
