namespace Bloomdo.Shared.DTOs.Chat;

public sealed class TodayLocalContext
{
    public int TotalScreenTimeSeconds { get; set; }
    public int Pickups { get; set; }
    public List<TodayAppUsageDto> TopApps { get; set; } = [];
}

public sealed class TodayAppUsageDto
{
    public string AppName { get; set; } = string.Empty;
    public int ForegroundSeconds { get; set; }
}
