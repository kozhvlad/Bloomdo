namespace Bloomdo.Shared.DTOs.Blocks;

public sealed class UpdateBlockRuleRequest
{
    public string? Title { get; init; }
    public bool? IsActive { get; init; }
    public List<string>? BlockedPackages { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public List<DayOfWeek>? Days { get; init; }
    public int? DailyLimitMinutes { get; init; }
    public int? FocusDurationMinutes { get; init; }
    public Guid? RequiredActivityGroupId { get; init; }
}
