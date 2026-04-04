using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Social;

public record SharedGroupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Icon { get; init; } = "📋";
    public string Color { get; init; } = "#7E57C2";
    public int MembersCount { get; init; }
    public int TasksCount { get; init; }
    public List<ProfileSummaryDto> MemberAvatars { get; init; } = [];
    public bool IsOwner { get; init; }
    public GroupMemberStatus MyStatus { get; init; }
}
