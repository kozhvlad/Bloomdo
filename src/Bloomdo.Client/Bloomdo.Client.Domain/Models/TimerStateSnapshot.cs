namespace Bloomdo.Client.Domain.Models;

public class TimerStateSnapshot
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskIcon { get; set; } = "✨";
    public string TaskColor { get; set; } = "#FF9800";
    public int TotalSeconds { get; set; }
    public int RemainingSeconds { get; set; }
    public int DurationMinutes { get; set; }
    public int Streak { get; set; }
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public DateTime LastTickUtc { get; set; }
    public DateOnly Date { get; set; }
}
