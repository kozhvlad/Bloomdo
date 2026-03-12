using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Domain.Entities;

public class BlockRule : BaseEntity
{
    public Guid AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public BlockType Type { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON-serialized list of blocked package names.
    /// </summary>
    public string BlockedPackagesJson { get; set; } = "[]";

    // Schedule
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// JSON-serialized list of DayOfWeek values.
    /// </summary>
    public string? ScheduleDaysJson { get; set; }

    // Limit
    public int? DailyLimitMinutes { get; set; }

    // Focus
    public int? FocusDurationMinutes { get; set; }

    /// <summary>
    /// UTC timestamp when a Focus session was activated. Null when not running.
    /// </summary>
    public DateTime? FocusStartedAtUtc { get; set; }

    public Account Account { get; set; } = null!;
}
