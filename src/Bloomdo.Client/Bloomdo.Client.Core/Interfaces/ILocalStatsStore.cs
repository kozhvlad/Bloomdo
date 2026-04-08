using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Persists stats data locally (calendar, weekly, daily) so the user
/// can view charts and history while offline. Retains up to 30 days.
/// </summary>
public interface ILocalStatsStore
{
    Task SaveMonthCalendarAsync(int year, int month, MonthCalendarResponse data);
    Task<MonthCalendarResponse?> LoadMonthCalendarAsync(int year, int month);

    Task SaveWeeklyStatsAsync(DateOnly weekStart, WeeklyStatsResponse data);
    Task<WeeklyStatsResponse?> LoadWeeklyStatsAsync(DateOnly weekStart);

    Task SaveDailyStatsAsync(DateOnly date, DailyStatsResponse data);
    Task<DailyStatsResponse?> LoadDailyStatsAsync(DateOnly date);

    /// <summary>
    /// Removes cached files older than <paramref name="days"/> days.
    /// </summary>
    Task CleanupAsync(int days = 30);
}
