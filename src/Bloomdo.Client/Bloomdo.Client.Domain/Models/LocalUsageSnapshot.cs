namespace Bloomdo.Client.Domain.Models;

/// <summary>
/// Locally cached daily usage data, persisted to survive app/device restarts.
/// </summary>
public sealed class LocalUsageSnapshot
{
    public DateOnly Date { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
    public int Pickups { get; set; }
    public bool SyncedToServer { get; set; }
    public List<LocalAppUsageEntry> Apps { get; set; } = [];
}

public sealed class LocalAppUsageEntry
{
    public string PackageName { get; set; } = string.Empty;
    public string? AppLabel { get; set; }
    public int ForegroundSeconds { get; set; }
}
