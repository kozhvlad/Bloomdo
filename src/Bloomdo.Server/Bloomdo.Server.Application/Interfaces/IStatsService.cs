using Bloomdo.Shared.DTOs.Stats;

namespace Bloomdo.Server.Application.Interfaces;

public interface IStatsService
{
    Task SyncUsageAsync(Guid accountId, SyncUsageRequest request, CancellationToken ct = default);
    Task<DailyStatsResponse?> GetDailyStatsAsync(Guid accountId, DateOnly date, CancellationToken ct = default);
    Task<MonthCalendarResponse> GetMonthCalendarAsync(Guid accountId, int year, int month, CancellationToken ct = default);
}
