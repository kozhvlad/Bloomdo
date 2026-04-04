using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Shared.DTOs.Social;

public record SharedGroupDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Icon { get; init; } = "📋";
    public string Color { get; init; } = "#7E57C2";
    public bool IsOwner { get; init; }
    public List<GroupMembershipDto> Members { get; init; } = [];
    public List<DailyActivityItemDto> Items { get; init; } = [];
    public List<GroupMemberProgressDto> MemberProgresses { get; init; } = [];
}
