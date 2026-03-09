using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data.Repositories;

public class StatsRepository(AppDbContext context) : IStatsRepository
{
    public async Task<AppUsageRecord?> GetUsageRecordAsync(Guid accountId, DateOnly date, string packageName, CancellationToken ct = default)
    {
        return await context.AppUsageRecords
            .FirstOrDefaultAsync(r => r.AccountId == accountId
                                      && r.Date == date
                                      && r.PackageName == packageName
                                      && !r.IsDeleted, ct);
    }

    public async Task<DailySnapshot?> GetSnapshotAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        return await context.DailySnapshots
            .FirstOrDefaultAsync(s => s.AccountId == accountId && s.Date == date && !s.IsDeleted, ct);
    }

    public async Task<List<DailySnapshot>> GetSnapshotsForMonthAsync(Guid accountId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        return await context.DailySnapshots
            .AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Date >= start && s.Date <= end && !s.IsDeleted)
            .OrderBy(s => s.Date)
            .ToListAsync(ct);
    }

    public async Task<List<AppUsageRecord>> GetUsageRecordsForDateAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        return await context.AppUsageRecords
            .AsNoTracking()
            .Where(r => r.AccountId == accountId && r.Date == date && !r.IsDeleted)
            .OrderByDescending(r => r.ForegroundSeconds)
            .ToListAsync(ct);
    }

    public async Task<List<DateOnly>> GetGoalMetDatesAsync(Guid accountId, CancellationToken ct = default)
    {
        return await context.DailySnapshots
            .AsNoTracking()
            .Where(s => s.AccountId == accountId && s.GoalMet && !s.IsDeleted)
            .OrderByDescending(s => s.Date)
            .Select(s => s.Date)
            .ToListAsync(ct);
    }

    public async Task AddUsageRecordAsync(AppUsageRecord record, CancellationToken ct = default)
    {
        await context.AppUsageRecords.AddAsync(record, ct);
    }

    public async Task AddSnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
    {
        await context.DailySnapshots.AddAsync(snapshot, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
