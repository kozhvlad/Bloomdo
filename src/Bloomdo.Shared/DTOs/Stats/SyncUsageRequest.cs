namespace Bloomdo.Shared.DTOs.Stats;

public sealed class SyncUsageRequest
{
    public DateOnly Date { get; init; }
    public int Pickups { get; init; }
    public List<AppUsageEntry> Apps { get; init; } = [];
}
