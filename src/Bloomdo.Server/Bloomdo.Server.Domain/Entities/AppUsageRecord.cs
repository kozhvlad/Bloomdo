namespace Bloomdo.Server.Domain.Entities;

public class AppUsageRecord : BaseEntity
{
    public Guid AccountId { get; set; }
    public DateOnly Date { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string? AppLabel { get; set; }
    public int ForegroundSeconds { get; set; }

    public Account Account { get; set; } = null!;
}
