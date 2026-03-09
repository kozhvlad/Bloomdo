using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Achievements;

namespace Bloomdo.Server.Application.Services;

public class AchievementService(
    IRepository<Achievement> achievementRepository,
    IRepository<AccountAchievement> accountAchievementRepository,
    IStatsRepository statsRepository) : IAchievementService
{
    private static readonly (string Code, string Title, string Description, string Icon, Func<int, bool> Condition)[] AchievementDefinitions =
    [
        ("streak_3", "Getting Started", "3-day streak", "🔥", streak => streak >= 3),
        ("streak_7", "Week Warrior", "7-day streak", "⚡", streak => streak >= 7),
        ("streak_14", "Two Weeks Strong", "14-day streak", "💪", streak => streak >= 14),
        ("streak_30", "Monthly Master", "30-day streak", "🏆", streak => streak >= 30),
        ("streak_100", "Century Club", "100-day streak", "👑", streak => streak >= 100),
    ];

    public async Task<List<AchievementResponse>> GetAchievementsAsync(Guid accountId, CancellationToken ct = default)
    {
        var achievements = (await achievementRepository.GetAllAsync(ct)).ToList();
        var unlocked = (await accountAchievementRepository.FindAsync(a => a.AccountId == accountId, ct))
            .ToDictionary(a => a.AchievementId);

        return achievements
            .OrderBy(a => a.SortOrder)
            .Select(a =>
            {
                unlocked.TryGetValue(a.Id, out var ua);
                return new AchievementResponse
                {
                    Id = a.Id,
                    Code = a.Code,
                    Title = a.Title,
                    Description = a.Description,
                    Icon = a.Icon,
                    IsUnlocked = ua is not null,
                    UnlockedAt = ua?.UnlockedDate
                };
            })
            .ToList();
    }

    public async Task EvaluateAchievementsAsync(Guid accountId, CancellationToken ct = default)
    {
        var goalDays = await statsRepository.GetGoalMetDatesAsync(accountId, ct);
        if (goalDays.Count == 0) return;

        // Calculate current streak
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStreak = 0;
        var checkDate = goalDays.Contains(today) ? today : today.AddDays(-1);

        foreach (var day in goalDays)
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

        var achievements = (await achievementRepository.GetAllAsync(ct)).ToList();
        var existingUnlocks = (await accountAchievementRepository.FindAsync(a => a.AccountId == accountId, ct))
            .Select(a => a.AchievementId)
            .ToHashSet();

        foreach (var def in AchievementDefinitions)
        {
            if (!def.Condition(currentStreak)) continue;

            var achievement = achievements.FirstOrDefault(a => a.Code == def.Code);
            if (achievement is null || existingUnlocks.Contains(achievement.Id)) continue;

            await accountAchievementRepository.AddAsync(new AccountAchievement
            {
                AccountId = accountId,
                AchievementId = achievement.Id,
                UnlockedDate = today
            }, ct);
        }
    }
}
