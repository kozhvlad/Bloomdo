using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Interfaces;

public interface IStatsRepository
{
    Task<AppUsageRecord?> GetUsageRecordAsync(Guid accountId, DateOnly date, string packageName, CancellationToken ct = default);
    Task<DailySnapshot?> GetSnapshotAsync(Guid accountId, DateOnly date, CancellationToken ct = default);
    Task<List<DailySnapshot>> GetSnapshotsForMonthAsync(Guid accountId, DateOnly start, DateOnly end, CancellationToken ct = default);
    Task<List<AppUsageRecord>> GetUsageRecordsForDateAsync(Guid accountId, DateOnly date, CancellationToken ct = default);
    Task<List<DateOnly>> GetGoalMetDatesAsync(Guid accountId, CancellationToken ct = default);
    Task AddUsageRecordAsync(AppUsageRecord record, CancellationToken ct = default);
    Task AddSnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
