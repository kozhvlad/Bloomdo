namespace Bloomdo.Shared.DTOs.Stats;

public sealed class CalendarDayDto
{
    public DateOnly Date { get; init; }
    public bool GoalMet { get; init; }
    public bool IsFreezeDay { get; init; }
    public int TotalScreenTimeSeconds { get; init; }
}
