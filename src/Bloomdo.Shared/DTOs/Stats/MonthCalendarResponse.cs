namespace Bloomdo.Shared.DTOs.Stats;

public sealed class MonthCalendarResponse
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public List<CalendarDayDto> Days { get; init; } = [];
}
