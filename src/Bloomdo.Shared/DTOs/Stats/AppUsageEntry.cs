namespace Bloomdo.Shared.DTOs.Stats;

public sealed class AppUsageEntry
{
    public string PackageName { get; init; } = string.Empty;
    public string? AppLabel { get; init; }
    public int ForegroundSeconds { get; init; }
}
