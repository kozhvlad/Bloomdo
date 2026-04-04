using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Social;

public record FollowStatusDto
{
    public ProfileSummaryDto User { get; init; } = null!;
    public bool IsFollowing { get; init; }
    public bool IsFollower { get; init; }
    public Guid? FollowId { get; init; }
    public bool IsPending { get; init; }
    public bool IsPrivateProfile { get; init; }
}
