using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Blocks;

public sealed class BlockRuleResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public BlockType Type { get; init; }
    public bool IsActive { get; init; }
    public List<string> BlockedPackages { get; init; } = [];

    // Schedule-specific
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public List<DayOfWeek>? Days { get; init; }

    // Limit-specific
    public int? DailyLimitMinutes { get; init; }

    // Focus-specific
    public int? FocusDurationMinutes { get; init; }
}
