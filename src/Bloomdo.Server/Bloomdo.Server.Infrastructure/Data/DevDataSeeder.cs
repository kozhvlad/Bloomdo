using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bloomdo.Server.Infrastructure.Data;

/// <summary>
/// Seeds test accounts with realistic data for development/testing.
/// </summary>
public static class DevDataSeeder
{
    private static readonly Guid SeedAccountId = new("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid DemoAccountId = new("d0000000-0000-0000-0000-000000000002");

    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        await SeedTestAccountAsync(context, logger);
        await SeedDemoAccountAsync(context, logger);
    }

    /// <summary>
    /// Original test account: test@bloomdo.dev / Test123!
    /// </summary>
    private static async Task SeedTestAccountAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == SeedAccountId))
        {
            logger?.LogInformation("Seed account already exists, skipping");
            return;
        }

        logger?.LogInformation("Seeding test account and statistics...");

        var now = DateTime.UtcNow;

        var account = new Account
        {
            Id = SeedAccountId,
            Email = "test@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "Test",
            LastName = "User",
            Username = "testuser",
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-45)
        };
        context.Accounts.Add(account);

        context.AccountRoles.Add(new AccountRole
        {
            Id = Guid.NewGuid(),
            AccountId = SeedAccountId,
            RoleId = (int)UserRole.User,
            CreatedAt = now
        });

        context.BlockRules.Add(new BlockRule
        {
            Id = Guid.NewGuid(),
            AccountId = SeedAccountId,
            Title = "Social media limit",
            Type = BlockType.Limit,
            IsActive = true,
            DailyLimitMinutes = 60,
            BlockedPackagesJson = "[\"com.instagram.android\",\"com.twitter.android\",\"com.zhiliaoapp.musically\"]",
            CreatedAt = now
        });

        var random = new Random(42);
        var today = DateOnly.FromDateTime(now);

        var apps = new (string Package, string Label)[]
        {
            ("com.instagram.android", "Instagram"),
            ("com.twitter.android", "Twitter"),
            ("com.whatsapp", "WhatsApp"),
            ("com.google.android.youtube", "YouTube"),
            ("com.zhiliaoapp.musically", "TikTok"),
            ("com.spotify.music", "Spotify"),
            ("com.google.android.gm", "Gmail"),
            ("org.telegram.messenger", "Telegram"),
            ("com.google.android.apps.maps", "Maps"),
            ("com.reddit.frontpage", "Reddit")
        };

        var weeklyBaselines = new[] { 6.0, 5.2, 4.5, 3.8, 3.5 };

        for (var dayOffset = 34; dayOffset >= 0; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            var weekIndex = dayOffset / 7;
            var reversedWeekIndex = 4 - weekIndex;
            if (reversedWeekIndex >= weeklyBaselines.Length)
                reversedWeekIndex = weeklyBaselines.Length - 1;

            var baselineHours = weeklyBaselines[reversedWeekIndex];
            var variation = 0.7 + random.NextDouble() * 0.6;
            var totalHours = baselineHours * variation;
            var totalSeconds = (int)(totalHours * 3600);
            var goalMet = totalHours < 4.0;
            var pickups = (int)(totalHours * 8 + random.Next(-5, 15));

            context.DailySnapshots.Add(new DailySnapshot
            {
                Id = Guid.NewGuid(),
                AccountId = SeedAccountId,
                Date = date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = Math.Max(1, pickups),
                GoalMet = goalMet,
                CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
            });

            var remainingSeconds = totalSeconds;
            var appsForDay = apps.OrderBy(_ => random.Next()).Take(random.Next(4, 8)).ToArray();

            for (var a = 0; a < appsForDay.Length; a++)
            {
                int appSeconds;
                if (a == appsForDay.Length - 1)
                {
                    appSeconds = remainingSeconds;
                }
                else
                {
                    var share = (appsForDay.Length - a) / (double)appsForDay.Length;
                    appSeconds = (int)(remainingSeconds * share * (0.3 + random.NextDouble() * 0.4));
                    appSeconds = Math.Min(appSeconds, remainingSeconds);
                }

                if (appSeconds <= 0) continue;
                remainingSeconds -= appSeconds;

                context.AppUsageRecords.Add(new AppUsageRecord
                {
                    Id = Guid.NewGuid(),
                    AccountId = SeedAccountId,
                    Date = date,
                    PackageName = appsForDay[a].Package,
                    AppLabel = appsForDay[a].Label,
                    ForegroundSeconds = appSeconds,
                    CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                });
            }
        }

        var studyGroupId = Guid.NewGuid();
        var sportGroupId = Guid.NewGuid();
        var selfCareGroupId = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = studyGroupId, AccountId = SeedAccountId, Title = "Study", Icon = "📚", Color = "#42A5F5", SortOrder = 1, CreatedAt = now },
            new ActivityGroup { Id = sportGroupId, AccountId = SeedAccountId, Title = "Sport", Icon = "🏃", Color = "#66BB6A", SortOrder = 2, CreatedAt = now },
            new ActivityGroup { Id = selfCareGroupId, AccountId = SeedAccountId, Title = "Self-Care", Icon = "🧘", Color = "#FF9800", SortOrder = 3, CreatedAt = now }
        );

        var englishItemId = Guid.NewGuid();
        var readItemId = Guid.NewGuid();
        var mathItemId = Guid.NewGuid();
        var runItemId = Guid.NewGuid();
        var stretchItemId = Guid.NewGuid();
        var meditateItemId = Guid.NewGuid();
        var journalItemId = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = englishItemId, ActivityGroupId = studyGroupId, Title = "English lesson", DurationMinutes = 60, Icon = "🇬🇧", Color = "#42A5F5", SortOrder = 1, CreatedAt = now },
            new ActivityItem { Id = readItemId, ActivityGroupId = studyGroupId, Title = "Read 30 pages", Description = "Any non-fiction book", Icon = "📖", Color = "#7E57C2", SortOrder = 2, CreatedAt = now },
            new ActivityItem { Id = mathItemId, ActivityGroupId = studyGroupId, Title = "Practice math", DurationMinutes = 45, Icon = "🧮", Color = "#5C6BC0", SortOrder = 3, CreatedAt = now },
            new ActivityItem { Id = runItemId, ActivityGroupId = sportGroupId, Title = "Morning run", DurationMinutes = 30, Description = "5 km minimum", Icon = "🏃", Color = "#66BB6A", SortOrder = 1, CreatedAt = now },
            new ActivityItem { Id = stretchItemId, ActivityGroupId = sportGroupId, Title = "Evening stretching", DurationMinutes = 15, Icon = "🤸", Color = "#26C6DA", SortOrder = 2, CreatedAt = now },
            new ActivityItem { Id = meditateItemId, ActivityGroupId = selfCareGroupId, Title = "Meditate", DurationMinutes = 10, Icon = "🧘", Color = "#AB47BC", SortOrder = 1, CreatedAt = now },
            new ActivityItem { Id = journalItemId, ActivityGroupId = selfCareGroupId, Title = "Write journal", Description = "Reflect on the day", Icon = "✍️", Color = "#FF9800", SortOrder = 2, CreatedAt = now }
        );

        var allItemIds = new[] { englishItemId, readItemId, mathItemId, runItemId, stretchItemId, meditateItemId, journalItemId };
        for (var dayOffset = 1; dayOffset <= 5; dayOffset++)
        {
            var completionDate = today.AddDays(-dayOffset);
            foreach (var itemId in allItemIds)
            {
                if (random.NextDouble() < 0.7)
                {
                    context.ActivityCompletions.Add(new ActivityCompletion
                    {
                        Id = Guid.NewGuid(),
                        ActivityItemId = itemId,
                        AccountId = SeedAccountId,
                        Date = completionDate,
                        CompletedAtUtc = completionDate.ToDateTime(new TimeOnly(random.Next(8, 22), random.Next(0, 60)), DateTimeKind.Utc),
                        CreatedAt = now
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Seed complete: test@bloomdo.dev / Test123!");
    }

    /// <summary>
    /// Demo showcase account: vlad@gmail.com / Vlad123!
    /// Full profile with avatar, bio, rich activity data, achievements, and block rules.
    /// </summary>
    private static async Task SeedDemoAccountAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == DemoAccountId))
        {
            logger?.LogInformation("Demo account already exists, skipping");
            return;
        }

        logger?.LogInformation("Seeding demo account (vlad@gmail.com)...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var random = new Random(99);

        // ── 1. Account ───────────────────────────────────────────────
        var avatarJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            SkinTone = 1,
            BodyType = 1,
            EyeColor = 3,
            EyeStyle = 0,
            HairColor = 0,
            HairStyle = 1,
            GlassesStyle = 0,
            GlassesColor = 0,
            FacialHair = 0,
            FacialHairColor = 0,
            HeadwearStyle = 0,
            HeadwearColor = 0,
            ClothingStyle = 1,
            ClothingColor = 1,
            BackgroundColor = 1,
            MouthStyle = 0,
            FaceExtra = 0
        });

        var account = new Account
        {
            Id = DemoAccountId,
            Email = "vlad@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vlad123!"),
            FirstName = "Vlad",
            LastName = "Kozh",
            Username = "vladkozh",
            Bio = "Building better habits, one day at a time 🌱",
            AvatarJson = avatarJson,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-60)
        };
        context.Accounts.Add(account);

        // ── 2. Role ──────────────────────────────────────────────────
        context.AccountRoles.Add(new AccountRole
        {
            Id = Guid.NewGuid(),
            AccountId = DemoAccountId,
            RoleId = (int)UserRole.User,
            CreatedAt = now
        });

        // ── 3. Block Rules (3 rules) ────────────────────────────────
        context.BlockRules.AddRange(
            new BlockRule
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                Title = "Social media detox",
                Type = BlockType.Limit,
                IsActive = true,
                DailyLimitMinutes = 45,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.twitter.android\"]",
                CreatedAt = now.AddDays(-55)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                Title = "Morning focus",
                Type = BlockType.Focus,
                IsActive = true,
                FocusDurationMinutes = 90,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\",\"com.reddit.frontpage\"]",
                CreatedAt = now.AddDays(-50)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                Title = "Night mode",
                Type = BlockType.Schedule,
                IsActive = true,
                StartTime = new TimeOnly(23, 0),
                EndTime = new TimeOnly(7, 0),
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.google.android.youtube\",\"com.reddit.frontpage\"]",
                ScheduleDaysJson = "[0,1,2,3,4,5,6]",
                CreatedAt = now.AddDays(-40)
            }
        );

        // ── 4. Activity Groups & Items ───────────────────────────────
        var codingGroupId = Guid.NewGuid();
        var healthGroupId = Guid.NewGuid();
        var learningGroupId = Guid.NewGuid();
        var mindfulGroupId = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = codingGroupId, AccountId = DemoAccountId, Title = "Coding", Icon = "💻", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityGroup { Id = healthGroupId, AccountId = DemoAccountId, Title = "Health", Icon = "💪", Color = "#66BB6A", SortOrder = 2, CreatedAt = now.AddDays(-58) },
            new ActivityGroup { Id = learningGroupId, AccountId = DemoAccountId, Title = "Learning", Icon = "📚", Color = "#FF9800", SortOrder = 3, CreatedAt = now.AddDays(-55) },
            new ActivityGroup { Id = mindfulGroupId, AccountId = DemoAccountId, Title = "Mindfulness", Icon = "🧘", Color = "#AB47BC", SortOrder = 4, CreatedAt = now.AddDays(-50) }
        );

        // Coding tasks
        var codePracticeId = Guid.NewGuid();
        var sideProjectId = Guid.NewGuid();
        var codeReviewId = Guid.NewGuid();

        // Health tasks
        var workoutId = Guid.NewGuid();
        var walkId = Guid.NewGuid();
        var drinkWaterId = Guid.NewGuid();

        // Learning tasks
        var readBookId = Guid.NewGuid();
        var englishId = Guid.NewGuid();
        var watchCourseId = Guid.NewGuid();

        // Mindfulness tasks
        var meditateId = Guid.NewGuid();
        var journalId = Guid.NewGuid();

        context.ActivityItems.AddRange(
            // Coding
            new ActivityItem { Id = codePracticeId, ActivityGroupId = codingGroupId, Title = "LeetCode practice", DurationMinutes = 45, Description = "Solve 2 problems", Icon = "🧩", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = sideProjectId, ActivityGroupId = codingGroupId, Title = "Side project", DurationMinutes = 60, Description = "Work on Bloomdo features", Icon = "🚀", Color = "#5C6BC0", SortOrder = 2, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = codeReviewId, ActivityGroupId = codingGroupId, Title = "Code review", DurationMinutes = 30, Icon = "🔍", Color = "#26C6DA", SortOrder = 3, CreatedAt = now.AddDays(-55) },

            // Health
            new ActivityItem { Id = workoutId, ActivityGroupId = healthGroupId, Title = "Workout", DurationMinutes = 45, Description = "Gym or home workout", Icon = "🏋️", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = walkId, ActivityGroupId = healthGroupId, Title = "10k steps", Description = "Daily walking goal", Icon = "🚶", Color = "#4CAF50", SortOrder = 2, TaskType = 1, TargetCount = 10000, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = drinkWaterId, ActivityGroupId = healthGroupId, Title = "Drink water", Description = "8 glasses per day", Icon = "💧", Color = "#29B6F6", SortOrder = 3, TaskType = 1, TargetCount = 8, CreatedAt = now.AddDays(-55) },

            // Learning
            new ActivityItem { Id = readBookId, ActivityGroupId = learningGroupId, Title = "Read 20 pages", DurationMinutes = 30, Description = "Currently: Clean Architecture", Icon = "📖", Color = "#FF9800", SortOrder = 1, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = englishId, ActivityGroupId = learningGroupId, Title = "English practice", DurationMinutes = 30, Description = "Vocabulary & speaking", Icon = "🇬🇧", Color = "#EF5350", SortOrder = 2, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = watchCourseId, ActivityGroupId = learningGroupId, Title = "Watch course", DurationMinutes = 45, Description = "System Design / .NET deep dive", Icon = "🎓", Color = "#7E57C2", SortOrder = 3, CreatedAt = now.AddDays(-50) },

            // Mindfulness
            new ActivityItem { Id = meditateId, ActivityGroupId = mindfulGroupId, Title = "Meditate", DurationMinutes = 10, Description = "Morning meditation", Icon = "🧘", Color = "#AB47BC", SortOrder = 1, CreatedAt = now.AddDays(-50) },
            new ActivityItem { Id = journalId, ActivityGroupId = mindfulGroupId, Title = "Evening journal", DurationMinutes = 15, Description = "Gratitude & reflection", Icon = "✍️", Color = "#EC407A", SortOrder = 2, CreatedAt = now.AddDays(-50) }
        );

        // ── 5. Activity Completions (last 14 days — strong streaks) ──
        var allItems = new[]
        {
            codePracticeId, sideProjectId, codeReviewId,
            workoutId, walkId, drinkWaterId,
            readBookId, englishId, watchCourseId,
            meditateId, journalId
        };

        // Completion probabilities per item (higher = more consistent)
        var completionChance = new Dictionary<Guid, double>
        {
            [codePracticeId] = 0.85,
            [sideProjectId] = 0.70,
            [codeReviewId] = 0.55,
            [workoutId] = 0.80,
            [walkId] = 0.90,
            [drinkWaterId] = 0.95,
            [readBookId] = 0.75,
            [englishId] = 0.65,
            [watchCourseId] = 0.50,
            [meditateId] = 0.80,
            [journalId] = 0.70
        };

        for (var dayOffset = 0; dayOffset <= 13; dayOffset++)
        {
            var completionDate = today.AddDays(-dayOffset);
            foreach (var itemId in allItems)
            {
                var chance = completionChance[itemId];
                // Reduce chance for older days slightly
                if (dayOffset > 7) chance *= 0.85;

                if (random.NextDouble() < chance)
                {
                    context.ActivityCompletions.Add(new ActivityCompletion
                    {
                        Id = Guid.NewGuid(),
                        ActivityItemId = itemId,
                        AccountId = DemoAccountId,
                        Date = completionDate,
                        CompletedAtUtc = completionDate.ToDateTime(
                            new TimeOnly(random.Next(7, 22), random.Next(0, 60)),
                            DateTimeKind.Utc),
                        CreatedAt = now
                    });
                }
            }
        }

        // ── 6. Daily Snapshots (45 days — steady improvement) ────────
        var demoApps = new (string Package, string Label)[]
        {
            ("com.instagram.android", "Instagram"),
            ("com.google.android.youtube", "YouTube"),
            ("com.zhiliaoapp.musically", "TikTok"),
            ("org.telegram.messenger", "Telegram"),
            ("com.whatsapp", "WhatsApp"),
            ("com.spotify.music", "Spotify"),
            ("com.reddit.frontpage", "Reddit"),
            ("com.twitter.android", "Twitter")
        };

        // Improving trend over weeks: 5.5h → 2.5h
        var demoBaselines = new[] { 5.5, 4.8, 4.0, 3.3, 2.8, 2.5, 2.3 };

        for (var dayOffset = 44; dayOffset >= 0; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            var weekIndex = Math.Min(dayOffset / 7, demoBaselines.Length - 1);
            var reversedWeek = demoBaselines.Length - 1 - weekIndex;

            var baselineHours = demoBaselines[reversedWeek];
            var variation = 0.75 + random.NextDouble() * 0.5;
            var totalHours = baselineHours * variation;
            var totalSeconds = (int)(totalHours * 3600);
            var goalMet = totalHours < 3.5;
            var pickups = (int)(totalHours * 7 + random.Next(-3, 10));

            context.DailySnapshots.Add(new DailySnapshot
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                Date = date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = Math.Max(1, pickups),
                GoalMet = goalMet,
                CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
            });

            var remainingSeconds = totalSeconds;
            var appsForDay = demoApps.OrderBy(_ => random.Next()).Take(random.Next(3, 7)).ToArray();

            for (var a = 0; a < appsForDay.Length; a++)
            {
                int appSeconds;
                if (a == appsForDay.Length - 1)
                {
                    appSeconds = remainingSeconds;
                }
                else
                {
                    var share = (appsForDay.Length - a) / (double)appsForDay.Length;
                    appSeconds = (int)(remainingSeconds * share * (0.3 + random.NextDouble() * 0.4));
                    appSeconds = Math.Min(appSeconds, remainingSeconds);
                }

                if (appSeconds <= 0) continue;
                remainingSeconds -= appSeconds;

                context.AppUsageRecords.Add(new AppUsageRecord
                {
                    Id = Guid.NewGuid(),
                    AccountId = DemoAccountId,
                    Date = date,
                    PackageName = appsForDay[a].Package,
                    AppLabel = appsForDay[a].Label,
                    ForegroundSeconds = appSeconds,
                    CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                });
            }
        }

        // ── 7. Achievements ──────────────────────────────────────────
        context.AccountAchievements.AddRange(
            new AccountAchievement
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                AchievementId = new Guid("a0000000-0000-0000-0000-000000000001"), // streak_3
                UnlockedDate = today.AddDays(-50),
                CreatedAt = now.AddDays(-50)
            },
            new AccountAchievement
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                AchievementId = new Guid("a0000000-0000-0000-0000-000000000002"), // streak_7
                UnlockedDate = today.AddDays(-40),
                CreatedAt = now.AddDays(-40)
            },
            new AccountAchievement
            {
                Id = Guid.NewGuid(),
                AccountId = DemoAccountId,
                AchievementId = new Guid("a0000000-0000-0000-0000-000000000003"), // streak_14
                UnlockedDate = today.AddDays(-25),
                CreatedAt = now.AddDays(-25)
            }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Demo seed complete: vlad@gmail.com / Vlad123! (60-day account, 4 activity groups, 11 tasks, 14 days completions, 3 achievements, 3 block rules)");
    }
}
