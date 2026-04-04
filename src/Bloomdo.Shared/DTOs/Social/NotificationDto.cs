using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Social;

public record NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public ProfileSummaryDto? Actor { get; init; }
    public Guid? ReferenceId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
