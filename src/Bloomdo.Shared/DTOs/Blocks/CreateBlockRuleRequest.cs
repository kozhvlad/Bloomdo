using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Blocks;

public sealed class CreateBlockRuleRequest
{
    public string Title { get; init; } = string.Empty;
    public BlockType Type { get; init; }
    public List<string> BlockedPackages { get; init; } = [];
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public List<DayOfWeek>? Days { get; init; }
    public int? DailyLimitMinutes { get; init; }
    public int? FocusDurationMinutes { get; init; }
}
