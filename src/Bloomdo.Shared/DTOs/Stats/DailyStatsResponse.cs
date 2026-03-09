namespace Bloomdo.Shared.DTOs.Stats;

public sealed class DailyStatsResponse
{
    public DateOnly Date { get; init; }
    public int TotalScreenTimeSeconds { get; init; }
    public int Pickups { get; init; }
    public bool GoalMet { get; init; }
    public List<AppUsageEntry> Apps { get; init; } = [];
}
