using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Shared.DTOs.Activities;

public class GroupMemberProgressDto
{
    public ProfileSummaryDto Account { get; set; } = null!;
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
    public List<MemberTaskCompletionDto> TaskDetails { get; set; } = [];
}

public class MemberTaskCompletionDto
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
