using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Client.Core.Interfaces;

public interface IStatsApiService
{
    Task<bool> SyncUsageAsync(SyncUsageRequest request, CancellationToken ct = default);
    Task<DailyStatsResponse?> GetDailyStatsAsync(DateOnly date, CancellationToken ct = default);
    Task<MonthCalendarResponse?> GetMonthCalendarAsync(int year, int month, CancellationToken ct = default);
}
