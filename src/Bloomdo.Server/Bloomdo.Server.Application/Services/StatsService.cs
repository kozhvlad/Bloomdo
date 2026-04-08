using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Stats;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Services;

public class StatsService(
    IStatsRepository statsRepository,
    IRepository<ActivityGroup> groupRepository,
    IRepository<ActivityItem> itemRepository,
    IRepository<ActivityCompletion> completionRepository,
    IRepository<GroupMembership> membershipRepository,
    ISubscriptionService subscriptionService,
    IRepository<StreakFreeze> streakFreezeRepository,
    IFreeLimitsSettings freeLimitsSettings) : IStatsService
{
    public async Task SyncUsageAsync(Guid accountId, SyncUsageRequest request, CancellationToken ct = default)
    {
        foreach (var app in request.Apps)
        {
            var existing = await statsRepository.GetUsageRecordAsync(accountId, request.Date, app.PackageName, ct);

            if (existing is not null)
            {
                existing.ForegroundSeconds = app.ForegroundSeconds;
                existing.AppLabel = app.AppLabel ?? existing.AppLabel;
            }
            else
            {
                await statsRepository.AddUsageRecordAsync(new AppUsageRecord
                {
                    AccountId = accountId,
                    Date = request.Date,
                    PackageName = app.PackageName,
                    AppLabel = app.AppLabel,
                    ForegroundSeconds = app.ForegroundSeconds
                }, ct);
            }
        }

        var totalSeconds = request.Apps.Sum(a => a.ForegroundSeconds);
        var snapshot = await statsRepository.GetSnapshotAsync(accountId, request.Date, ct);

        var goalMet = await EvaluateAllTasksCompletedAsync(accountId, request.Date, ct);

        if (snapshot is not null)
        {
            snapshot.TotalScreenTimeSeconds = totalSeconds;
            snapshot.Pickups = request.Pickups;
            snapshot.GoalMet = goalMet;
        }
        else
        {
            await statsRepository.AddSnapshotAsync(new DailySnapshot
            {
                AccountId = accountId,
                Date = request.Date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = request.Pickups,
                GoalMet = goalMet
            }, ct);
        }

        await statsRepository.SaveChangesAsync(ct);
    }

    public async Task<DailyStatsResponse?> GetDailyStatsAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        var snapshot = await statsRepository.GetSnapshotAsync(accountId, date, ct);
        if (snapshot is null)
            return null;

        var records = await statsRepository.GetUsageRecordsForDateAsync(accountId, date, ct);

        return new DailyStatsResponse
        {
            Date = date,
            TotalScreenTimeSeconds = snapshot.TotalScreenTimeSeconds,
            Pickups = snapshot.Pickups,
            GoalMet = snapshot.GoalMet,
            Apps = records
                .OrderByDescending(r => r.ForegroundSeconds)
                .Select(r => new AppUsageEntry
                {
                    PackageName = r.PackageName,
                    AppLabel = r.AppLabel,
                    ForegroundSeconds = r.ForegroundSeconds
                })
                .ToList()
        };
    }

    public async Task<MonthCalendarResponse> GetMonthCalendarAsync(Guid accountId, int year, int month, CancellationToken ct = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Auto-apply streak freezes for premium users before calculating
        await AutoApplyStreakFreezesAsync(accountId, ct);

        var snapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, startDate, endDate, ct);
        var freezeDates = await GetFreezeDatesAsync(accountId, startDate, endDate, ct);

        var days = snapshots.Select(s => new CalendarDayDto
        {
            Date = s.Date,
            GoalMet = s.GoalMet || freezeDates.Contains(s.Date),
            IsFreezeDay = freezeDates.Contains(s.Date),
            TotalScreenTimeSeconds = s.TotalScreenTimeSeconds
        }).ToList();

        // Add freeze days that don't have snapshots
        foreach (var freezeDate in freezeDates)
        {
            if (days.All(d => d.Date != freezeDate))
            {
                days.Add(new CalendarDayDto
                {
                    Date = freezeDate,
                    GoalMet = true,
                    IsFreezeDay = true,
                    TotalScreenTimeSeconds = 0
                });
            }
        }

        var (current, longest) = await CalculateStreaksAsync(accountId, ct);

        return new MonthCalendarResponse
        {
            Year = year,
            Month = month,
            CurrentStreak = current,
            LongestStreak = longest,
            Days = days
        };
    }

    public async Task<WeeklyStatsResponse?> GetWeeklyStatsAsync(Guid accountId, DateOnly weekStartDate, CancellationToken ct = default)
    {
        var weekEndDate = weekStartDate.AddDays(6);

        var snapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, weekStartDate, weekEndDate, ct);
        var snapshotDict = snapshots.ToDictionary(s => s.Date);

        var dailyData = new List<DailyScreenTimeDto>();

        for (var i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            var hasData = snapshotDict.TryGetValue(date, out var snapshot);

            dailyData.Add(new DailyScreenTimeDto
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                ScreenTimeSeconds = hasData ? snapshot!.TotalScreenTimeSeconds : 0,
                Pickups = hasData ? snapshot!.Pickups : 0,
                GoalMet = hasData && snapshot!.GoalMet
            });
        }

        var totalSeconds = dailyData.Sum(d => d.ScreenTimeSeconds);
        var daysWithData = dailyData.Count(d => d.ScreenTimeSeconds > 0);
        var avgSeconds = daysWithData > 0 ? totalSeconds / daysWithData : 0;

        var totalPickups = dailyData.Sum(d => d.Pickups);
        var avgPickups = daysWithData > 0 ? totalPickups / daysWithData : 0;

        // Get comparison with previous week
        var prevWeekStart = weekStartDate.AddDays(-7);
        var prevWeekEnd = prevWeekStart.AddDays(6);
        var prevSnapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, prevWeekStart, prevWeekEnd, ct);

        WeekComparisonDto? comparison = null;
        if (prevSnapshots.Count > 0)
        {
            var prevTotalSeconds = prevSnapshots.Sum(s => s.TotalScreenTimeSeconds);
            var prevTotalPickups = prevSnapshots.Sum(s => s.Pickups);

            var screenTimeChange = totalSeconds - prevTotalSeconds;
            var screenTimeChangePercent = prevTotalSeconds > 0 
                ? (double)screenTimeChange / prevTotalSeconds * 100 
                : 0;

            var pickupsChange = totalPickups - prevTotalPickups;
            var pickupsChangePercent = prevTotalPickups > 0 
                ? (double)pickupsChange / prevTotalPickups * 100 
                : 0;

            comparison = new WeekComparisonDto
            {
                ScreenTimeChangePercent = screenTimeChangePercent,
                ScreenTimeChangeSeconds = screenTimeChange,
                PickupsChangePercent = pickupsChangePercent,
                PickupsChange = pickupsChange,
                IsImproving = screenTimeChange < 0
            };
        }

        // Get top apps for the week
        var usageRecords = await statsRepository.GetUsageRecordsForRangeAsync(accountId, weekStartDate, weekEndDate, ct);
        var topApps = usageRecords
            .GroupBy(r => r.PackageName)
            .Select(g => new AppUsageEntry
            {
                PackageName = g.Key,
                AppLabel = g.FirstOrDefault(r => !string.IsNullOrEmpty(r.AppLabel))?.AppLabel,
                ForegroundSeconds = g.Sum(r => r.ForegroundSeconds)
            })
            .OrderByDescending(a => a.ForegroundSeconds)
            .Take(10)
            .ToList();

        return new WeeklyStatsResponse
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            DailyData = dailyData,
            TotalScreenTimeSeconds = totalSeconds,
            AverageScreenTimeSeconds = avgSeconds,
            TotalPickups = totalPickups,
            AveragePickups = avgPickups,
            Comparison = comparison,
            TopApps = topApps
        };
    }

    private async Task<(int Current, int Longest)> CalculateStreaksAsync(Guid accountId, CancellationToken ct)
    {
        var goalDays = await statsRepository.GetGoalMetDatesAsync(accountId, ct);

        // Merge freeze days into goal days
        var allFreezeDates = await GetFreezeDatesAsync(accountId, DateOnly.MinValue, DateOnly.MaxValue, ct);
        var mergedDays = new SortedSet<DateOnly>(goalDays, Comparer<DateOnly>.Create((a, b) => b.CompareTo(a)));
        foreach (var fd in allFreezeDates)
            mergedDays.Add(fd);

        var sortedDays = mergedDays.ToList(); // descending

        if (sortedDays.Count == 0)
            return (0, 0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Current streak: count consecutive days ending at today or yesterday
        var currentStreak = 0;
        var checkDate = today;

        // If today is not in the list, start from yesterday
        if (!sortedDays.Contains(today) && sortedDays.Count > 0 && sortedDays[0] == today.AddDays(-1))
            checkDate = today.AddDays(-1);

        foreach (var day in sortedDays)
        {
            if (day == checkDate)
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (day < checkDate)
            {
                break;
            }
        }

        // Longest streak: scan all days (sorted descending)
        var longestStreak = 0;
        var streak = 1;

        for (var i = 1; i < sortedDays.Count; i++)
        {
            if (sortedDays[i - 1].AddDays(-1) == sortedDays[i])
            {
                streak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, streak);
                streak = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, streak);

        return (currentStreak, longestStreak);
    }

    /// <summary>
    /// Auto-applies streak freezes for premium users when a gap is detected
    /// in the recent streak.
    /// </summary>
    private async Task AutoApplyStreakFreezesAsync(Guid accountId, CancellationToken ct)
    {
        var isPremium = await subscriptionService.IsPremiumAsync(accountId, ct);
        if (!isPremium) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var goalDays = await statsRepository.GetGoalMetDatesAsync(accountId, ct);
        var goalDaySet = new HashSet<DateOnly>(goalDays);

        // Get existing freezes this month
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var existingFreezes = await streakFreezeRepository.FindAsync(
            f => f.AccountId == accountId && f.Date >= monthStart && f.Date <= monthEnd, ct);
        var freezeList = existingFreezes.ToList();
        var frozenDates = new HashSet<DateOnly>(freezeList.Select(f => f.Date));
        var freezesUsedThisMonth = freezeList.Count;

        var maxFreezes = freeLimitsSettings.MonthlyStreakFreezes;
        var remaining = maxFreezes - freezesUsedThisMonth;
        if (remaining <= 0) return;

        // Scan backwards from yesterday looking for gaps in the streak
        // Only freeze days adjacent to goal-met days (to preserve streak continuity)
        var checkDate = today.AddDays(-1);
        var frozenCount = 0;

        for (var i = 0; i < 7 && frozenCount < remaining; i++)
        {
            var date = checkDate.AddDays(-i);

            if (goalDaySet.Contains(date) || frozenDates.Contains(date))
                continue;

            // Check if freezing this day would connect to a goal-met day
            var hasPrevGoal = goalDaySet.Contains(date.AddDays(-1)) || frozenDates.Contains(date.AddDays(-1));
            var hasNextGoal = goalDaySet.Contains(date.AddDays(1)) || frozenDates.Contains(date.AddDays(1));

            if (hasPrevGoal || hasNextGoal)
            {
                await streakFreezeRepository.AddAsync(new StreakFreeze
                {
                    AccountId = accountId,
                    Date = date
                }, ct);
                frozenDates.Add(date);
                frozenCount++;
            }
            else
            {
                // No adjacent goal day — stop scanning
                break;
            }
        }
    }

    private async Task<HashSet<DateOnly>> GetFreezeDatesAsync(Guid accountId, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var freezes = await streakFreezeRepository.FindAsync(
            f => f.AccountId == accountId
                 && (start == DateOnly.MinValue || f.Date >= start)
                 && (end == DateOnly.MaxValue || f.Date <= end), ct);

        return [.. freezes.Select(f => f.Date)];
    }

    public async Task RecalculateGoalMetAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        var goalMet = await EvaluateAllTasksCompletedAsync(accountId, date, ct);
        var snapshot = await statsRepository.GetSnapshotAsync(accountId, date, ct);

        if (snapshot is not null)
        {
            snapshot.GoalMet = goalMet;
        }
        else
        {
            await statsRepository.AddSnapshotAsync(new DailySnapshot
            {
                AccountId = accountId,
                Date = date,
                TotalScreenTimeSeconds = 0,
                Pickups = 0,
                GoalMet = goalMet
            }, ct);
        }

        await statsRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// GoalMet = true when the user has at least one active task AND
    /// every active task across all owned and shared groups is completed for the given date.
    /// Count/Steps tasks require currentCount >= targetCount; other types require a completion record.
    /// </summary>
    private async Task<bool> EvaluateAllTasksCompletedAsync(Guid accountId, DateOnly date, CancellationToken ct)
    {
        var ownedGroups = await groupRepository.FindAsync(g => g.AccountId == accountId && g.IsActive, ct);

        var memberships = await membershipRepository.FindAsync(
            m => m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);
        var sharedGroupIds = memberships.Select(m => m.ActivityGroupId).ToHashSet();
        var sharedGroups = sharedGroupIds.Count > 0
            ? await groupRepository.FindAsync(g => sharedGroupIds.Contains(g.Id) && g.IsActive && g.AccountId != accountId, ct)
            : [];

        var allGroupIds = ownedGroups.Concat(sharedGroups).Select(g => g.Id).ToList();
        if (allGroupIds.Count == 0)
            return false;

        var allItems = new List<ActivityItem>();
        foreach (var gid in allGroupIds)
        {
            var items = await itemRepository.FindAsync(i => i.ActivityGroupId == gid && i.IsActive, ct);
            allItems.AddRange(items);
        }

        if (allItems.Count == 0)
            return false;

        var itemIds = allItems.Select(i => i.Id).ToHashSet();
        var completions = await completionRepository.FindAsync(
            c => c.AccountId == accountId && c.Date == date && itemIds.Contains(c.ActivityItemId), ct);
        var completionByItem = completions.ToDictionary(c => c.ActivityItemId);

        foreach (var item in allItems)
        {
            var isCountBased = item.TaskType == (int)ActivityItemType.Count
                            || item.TaskType == (int)ActivityItemType.Steps;

            if (isCountBased)
            {
                var currentCount = completionByItem.TryGetValue(item.Id, out var comp) ? comp.CountValue ?? 0 : 0;
                if (!item.TargetCount.HasValue || currentCount < item.TargetCount.Value)
                    return false;
            }
            else
            {
                if (!completionByItem.ContainsKey(item.Id))
                    return false;
            }
        }

        return true;
    }
}
