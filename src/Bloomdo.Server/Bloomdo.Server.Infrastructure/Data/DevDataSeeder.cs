using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bloomdo.Server.Infrastructure.Data;

/// <summary>
/// Seeds demo accounts with realistic data for product showcase.
///
/// ═══════════════════════════════════════════════════════════════════════════
///  DEMO ACCOUNTS  (login / password)
/// ═══════════════════════════════════════════════════════════════════════════
///
///  ── MAIN ACCOUNTS ──────────────────────────────────────────────────────
///   1. vlad@gmail.com        / Vlad123!      — Vlad (FREE), main demo, NO subscription, rich data
///   2. vladpro@gmail.com     / VladPro123!   — Vlad (PREMIUM), active subscription, rich data
///
///  ── DETAILED FRIENDS (4 fully developed accounts) ─────────────────────
///   3. mika@bloomdo.dev      / Mika123!      — Mika Sergeev, fitness &amp; sports
///   4. dasha@bloomdo.dev     / Dasha123!     — Dasha Orlova, student &amp; learning
///   5. artem@bloomdo.dev     / Artem123!     — Artem Bykov, programmer &amp; productivity
///   6. lena@bloomdo.dev      / Lena123!      — Lena Titova, creative &amp; art
///
///  ── EXTRA ACCOUNTS (10 filler accounts for friend lists) ──────────────
///   7.  nikita@bloomdo.dev   / Nikita123!    — Nikita Volkov
///   8.  sonya@bloomdo.dev    / Sonya123!     — Sonya Ivanova
///   9.  max@bloomdo.dev      / Max123!       — Max Petrov
///  10.  katya@bloomdo.dev    / Katya123!     — Katya Smirnova
///  11.  roma@bloomdo.dev     / Roma123!      — Roma Kuznetsov
///  12.  nastya@bloomdo.dev   / Nastya123!    — Nastya Popova
///  13.  igor@bloomdo.dev     / Igor123!      — Igor Novikov
///  14.  polina@bloomdo.dev   / Polina123!    — Polina Morozova
///  15.  dima@bloomdo.dev     / Dima123!      — Dima Sokolov
///  16.  anya@bloomdo.dev     / Anya123!      — Anya Fedorova
///
/// ═══════════════════════════════════════════════════════════════════════════
///  FRIENDSHIP GRAPH
/// ═══════════════════════════════════════════════════════════════════════════
///
///  Vlad Free  ↔ Mika, Dasha, Artem, Lena         (mutual — close friends)
///  Vlad Free  ↔ Nikita, Sonya, Max                (mutual)
///  Vlad Free  ← Igor                              (pending request)
///
///  Vlad Pro   ↔ Mika, Artem, Dasha, Lena          (mutual — close friends)
///  Vlad Pro   ↔ Katya, Roma, Nastya               (mutual)
///  Vlad Pro   ← Polina                            (pending request)
///
///  Mika       ↔ Artem, Dasha                      (mutual)
///  Dasha      ↔ Lena                              (mutual)
///  Nikita     ↔ Max, Roma                          (mutual)
///  Sonya      ↔ Katya, Anya                        (mutual)
///  Dima       ↔ Igor                               (mutual)
///
/// ═══════════════════════════════════════════════════════════════════════════
/// </summary>
public static class DevDataSeeder
{
    // ── Account IDs ──────────────────────────────────────────────────────
    private static readonly Guid VladFreeId    = new("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid VladPremiumId = new("d0000000-0000-0000-0000-000000000002");
    private static readonly Guid MikaId        = new("d0000000-0000-0000-0000-000000000003");
    private static readonly Guid DashaId       = new("d0000000-0000-0000-0000-000000000004");
    private static readonly Guid ArtemId       = new("d0000000-0000-0000-0000-000000000005");
    private static readonly Guid LenaId        = new("d0000000-0000-0000-0000-000000000006");
    private static readonly Guid NikitaId      = new("d0000000-0000-0000-0000-000000000007");
    private static readonly Guid SonyaId       = new("d0000000-0000-0000-0000-000000000008");
    private static readonly Guid MaxId         = new("d0000000-0000-0000-0000-000000000009");
    private static readonly Guid KatyaId       = new("d0000000-0000-0000-0000-00000000000a");
    private static readonly Guid RomaId        = new("d0000000-0000-0000-0000-00000000000b");
    private static readonly Guid NastyaId      = new("d0000000-0000-0000-0000-00000000000c");
    private static readonly Guid IgorId        = new("d0000000-0000-0000-0000-00000000000d");
    private static readonly Guid PolinaId      = new("d0000000-0000-0000-0000-00000000000e");
    private static readonly Guid DimaId        = new("d0000000-0000-0000-0000-00000000000f");
    private static readonly Guid AnyaId        = new("d0000000-0000-0000-0000-000000000010");

    // ── Achievement reference IDs (seeded in migration) ──────────────────
    private static readonly Guid AchStreak3   = new("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid AchStreak7   = new("a0000000-0000-0000-0000-000000000002");
    private static readonly Guid AchStreak14  = new("a0000000-0000-0000-0000-000000000003");
    private static readonly Guid AchStreak30  = new("a0000000-0000-0000-0000-000000000004");
    private static readonly Guid AchStreak100 = new("a0000000-0000-0000-0000-000000000005");

    // ── Common app list for screen time data ─────────────────────────────
    private static readonly (string Package, string Label)[] AllApps =
    [
        ("com.instagram.android", "Instagram"),
        ("com.google.android.youtube", "YouTube"),
        ("com.zhiliaoapp.musically", "TikTok"),
        ("org.telegram.messenger", "Telegram"),
        ("com.whatsapp", "WhatsApp"),
        ("com.spotify.music", "Spotify"),
        ("com.reddit.frontpage", "Reddit"),
        ("com.twitter.android", "Twitter"),
        ("com.google.android.gm", "Gmail"),
        ("com.discord", "Discord"),
        ("com.snapchat.android", "Snapchat"),
        ("com.pinterest", "Pinterest")
    ];

    // ═════════════════════════════════════════════════════════════════════
    //  PUBLIC ENTRY POINT
    // ═════════════════════════════════════════════════════════════════════

    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        await SeedVladFreeAsync(context, logger);
        await SeedVladPremiumAsync(context, logger);
        await SeedMikaAsync(context, logger);
        await SeedDashaAsync(context, logger);
        await SeedArtemAsync(context, logger);
        await SeedLenaAsync(context, logger);
        await SeedExtraAccountsAsync(context, logger);
        await SeedFriendshipsAsync(context, logger);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  1. VLAD (FREE) — main demo account, NO subscription
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedVladFreeAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == VladFreeId))
        {
            logger?.LogInformation("Vlad (Free) already exists, skipping");
            return;
        }

        logger?.LogInformation("Seeding Vlad (Free): vlad@gmail.com ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(42);

        // ── Account ──────────────────────────────────────────────────
        context.Accounts.Add(new Account
        {
            Id = VladFreeId,
            Email = "vlad@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vlad123!"),
            FirstName = "Vlad",
            LastName = "Kozh",
            Username = "vladkozh",
            Bio = "Building better habits, one day at a time 🌱",
            AvatarJson = MakeAvatar(1, 1, 3, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0),
            ProfileVisibility = ProfileVisibility.FriendsOnly,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-60)
        });

        context.AccountRoles.Add(new AccountRole
        {
            Id = Guid.NewGuid(),
            AccountId = VladFreeId,
            RoleId = (int)UserRole.User,
            CreatedAt = now
        });

        // ── Block Rules (3) ─────────────────────────────────────────
        context.BlockRules.AddRange(
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladFreeId,
                Title = "Social media detox", Type = BlockType.Limit, IsActive = true,
                DailyLimitMinutes = 45,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.twitter.android\"]",
                CreatedAt = now.AddDays(-55)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladFreeId,
                Title = "Morning focus", Type = BlockType.Focus, IsActive = true,
                FocusDurationMinutes = 90,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\",\"com.reddit.frontpage\"]",
                CreatedAt = now.AddDays(-50)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladFreeId,
                Title = "Night mode", Type = BlockType.Schedule, IsActive = true,
                StartTime = new TimeOnly(23, 0), EndTime = new TimeOnly(7, 0),
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.google.android.youtube\",\"com.reddit.frontpage\"]",
                ScheduleDaysJson = "[0,1,2,3,4,5,6]",
                CreatedAt = now.AddDays(-40)
            }
        );

        // ── Activity Groups & Items ─────────────────────────────────
        var codingGrp = Guid.NewGuid();
        var healthGrp = Guid.NewGuid();
        var learningGrp = Guid.NewGuid();
        var mindfulGrp = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = codingGrp, AccountId = VladFreeId, Title = "Coding", Icon = "💻", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityGroup { Id = healthGrp, AccountId = VladFreeId, Title = "Health", Icon = "💪", Color = "#66BB6A", SortOrder = 2, CreatedAt = now.AddDays(-58) },
            new ActivityGroup { Id = learningGrp, AccountId = VladFreeId, Title = "Learning", Icon = "📚", Color = "#FF9800", SortOrder = 3, CreatedAt = now.AddDays(-55) },
            new ActivityGroup { Id = mindfulGrp, AccountId = VladFreeId, Title = "Mindfulness", Icon = "🧘", Color = "#AB47BC", SortOrder = 4, CreatedAt = now.AddDays(-50) }
        );

        var leetcodeId    = Guid.NewGuid();
        var sideProjectId = Guid.NewGuid();
        var codeReviewId  = Guid.NewGuid();
        var workoutId     = Guid.NewGuid();
        var walkId        = Guid.NewGuid();
        var waterId       = Guid.NewGuid();
        var readBookId    = Guid.NewGuid();
        var englishId     = Guid.NewGuid();
        var courseId       = Guid.NewGuid();
        var meditateId    = Guid.NewGuid();
        var journalId     = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = leetcodeId, ActivityGroupId = codingGrp, Title = "LeetCode practice", DurationMinutes = 45, Description = "Solve 2 problems", Icon = "🧩", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = sideProjectId, ActivityGroupId = codingGrp, Title = "Side project", DurationMinutes = 60, Description = "Work on Bloomdo features", Icon = "🚀", Color = "#5C6BC0", SortOrder = 2, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = codeReviewId, ActivityGroupId = codingGrp, Title = "Code review", DurationMinutes = 30, Icon = "🔍", Color = "#26C6DA", SortOrder = 3, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = workoutId, ActivityGroupId = healthGrp, Title = "Workout", DurationMinutes = 45, Description = "Gym or home workout", Icon = "🏋️", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = walkId, ActivityGroupId = healthGrp, Title = "10k steps", Description = "Daily walking goal", Icon = "🚶", Color = "#4CAF50", SortOrder = 2, TaskType = (int)ActivityItemType.Steps, TargetCount = 10000, CreatedAt = now.AddDays(-58) },
            new ActivityItem { Id = waterId, ActivityGroupId = healthGrp, Title = "Drink water", Description = "8 glasses per day", Icon = "💧", Color = "#29B6F6", SortOrder = 3, TaskType = (int)ActivityItemType.Count, TargetCount = 8, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = readBookId, ActivityGroupId = learningGrp, Title = "Read 20 pages", DurationMinutes = 30, Description = "Currently: Clean Architecture", Icon = "📖", Color = "#FF9800", SortOrder = 1, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = englishId, ActivityGroupId = learningGrp, Title = "English practice", DurationMinutes = 30, Description = "Vocabulary & speaking", Icon = "🇬🇧", Color = "#EF5350", SortOrder = 2, CreatedAt = now.AddDays(-55) },
            new ActivityItem { Id = courseId, ActivityGroupId = learningGrp, Title = "Watch course", DurationMinutes = 45, Description = "System Design / .NET deep dive", Icon = "🎓", Color = "#7E57C2", SortOrder = 3, CreatedAt = now.AddDays(-50) },
            new ActivityItem { Id = meditateId, ActivityGroupId = mindfulGrp, Title = "Meditate", DurationMinutes = 10, Description = "Morning meditation", Icon = "🧘", Color = "#AB47BC", SortOrder = 1, CreatedAt = now.AddDays(-50) },
            new ActivityItem { Id = journalId, ActivityGroupId = mindfulGrp, Title = "Evening journal", DurationMinutes = 15, Description = "Gratitude & reflection", Icon = "✍️", Color = "#EC407A", SortOrder = 2, CreatedAt = now.AddDays(-50) }
        );

        // ── Activity Completions (14 days, strong streaks) ──────────
        var itemChances = new Dictionary<Guid, double>
        {
            [leetcodeId]    = 0.85, [sideProjectId] = 0.70, [codeReviewId] = 0.55,
            [workoutId]     = 0.80, [walkId]        = 0.90, [waterId]      = 0.95,
            [readBookId]    = 0.75, [englishId]     = 0.65, [courseId]      = 0.50,
            [meditateId]    = 0.80, [journalId]     = 0.70
        };
        SeedCompletions(context, VladFreeId, itemChances, 14, today, now, rng);

        // ── Daily Snapshots + App Usage (45 days, improving: 5.5h → 2.5h) ──
        SeedScreenTime(context, VladFreeId, 45, [5.5, 4.8, 4.0, 3.3, 2.8, 2.5, 2.3], today, now, rng);

        // ── Achievements ─────────────────────────────────────────────
        context.AccountAchievements.AddRange(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladFreeId, AchievementId = AchStreak3,  UnlockedDate = today.AddDays(-50), CreatedAt = now.AddDays(-50) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladFreeId, AchievementId = AchStreak7,  UnlockedDate = today.AddDays(-40), CreatedAt = now.AddDays(-40) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladFreeId, AchievementId = AchStreak14, UnlockedDate = today.AddDays(-25), CreatedAt = now.AddDays(-25) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Vlad (Free) seed complete: vlad@gmail.com / Vlad123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  2. VLAD (PREMIUM) — same persona, active subscription
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedVladPremiumAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == VladPremiumId))
        {
            logger?.LogInformation("Vlad (Premium) already exists, skipping");
            return;
        }

        logger?.LogInformation("Seeding Vlad (Premium): vladpro@gmail.com ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(77);

        // ── Account ──────────────────────────────────────────────────
        context.Accounts.Add(new Account
        {
            Id = VladPremiumId,
            Email = "vladpro@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("VladPro123!"),
            FirstName = "Vlad",
            LastName = "Kozh",
            Username = "vladpro",
            Bio = "Premium mode 💎 Focused on growth every day",
            AvatarJson = MakeAvatar(1, 2, 3, 1, 0, 2, 0, 0, 0, 0, 0, 0, 2, 3, 4, 1, 0),
            ProfileVisibility = ProfileVisibility.Public,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-90)
        });

        context.AccountRoles.Add(new AccountRole
        {
            Id = Guid.NewGuid(), AccountId = VladPremiumId,
            RoleId = (int)UserRole.Premium, CreatedAt = now
        });

        // ── Subscription (Active Monthly) ────────────────────────────
        context.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), AccountId = VladPremiumId,
            StripeCustomerId = "cus_seed_vladpro", StripeSubscriptionId = "sub_seed_vladpro",
            Plan = SubscriptionPlan.Monthly, Status = SubscriptionStatus.Active,
            CurrentPeriodStart = now.AddDays(-15), CurrentPeriodEnd = now.AddDays(15),
            CancelAtPeriodEnd = false, CreatedAt = now.AddDays(-80)
        });

        // ── Block Rules (5 — premium = unlimited) ────────────────────
        context.BlockRules.AddRange(
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladPremiumId,
                Title = "Total social detox", Type = BlockType.Limit, IsActive = true,
                DailyLimitMinutes = 30,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.twitter.android\",\"com.snapchat.android\"]",
                CreatedAt = now.AddDays(-85)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladPremiumId,
                Title = "Deep work mornings", Type = BlockType.Focus, IsActive = true,
                FocusDurationMinutes = 120,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\",\"com.reddit.frontpage\",\"com.discord\"]",
                CreatedAt = now.AddDays(-80)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladPremiumId,
                Title = "Work hours shield", Type = BlockType.Schedule, IsActive = true,
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0),
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.google.android.youtube\"]",
                ScheduleDaysJson = "[1,2,3,4,5]",
                CreatedAt = now.AddDays(-70)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladPremiumId,
                Title = "Night shield", Type = BlockType.Schedule, IsActive = true,
                StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(7, 0),
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\",\"com.reddit.frontpage\",\"com.snapchat.android\"]",
                ScheduleDaysJson = "[0,1,2,3,4,5,6]",
                CreatedAt = now.AddDays(-60)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = VladPremiumId,
                Title = "YouTube limiter", Type = BlockType.Limit, IsActive = true,
                DailyLimitMinutes = 20,
                BlockedPackagesJson = "[\"com.google.android.youtube\"]",
                CreatedAt = now.AddDays(-50)
            }
        );

        // ── Activity Groups & Items (5 groups, 15 items) ─────────────
        var startupGrp      = Guid.NewGuid();
        var fitnessGrp      = Guid.NewGuid();
        var languagesGrp    = Guid.NewGuid();
        var creativeGrp     = Guid.NewGuid();
        var productivityGrp = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = startupGrp,      AccountId = VladPremiumId, Title = "Startup",      Icon = "🚀", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-88) },
            new ActivityGroup { Id = fitnessGrp,      AccountId = VladPremiumId, Title = "Fitness",      Icon = "🏋️", Color = "#66BB6A", SortOrder = 2, CreatedAt = now.AddDays(-88) },
            new ActivityGroup { Id = languagesGrp,    AccountId = VladPremiumId, Title = "Languages",    Icon = "🌍", Color = "#FF9800", SortOrder = 3, CreatedAt = now.AddDays(-85) },
            new ActivityGroup { Id = creativeGrp,     AccountId = VladPremiumId, Title = "Creative",     Icon = "✨", Color = "#EC407A", SortOrder = 4, CreatedAt = now.AddDays(-80) },
            new ActivityGroup { Id = productivityGrp, AccountId = VladPremiumId, Title = "Productivity", Icon = "🎯", Color = "#7E57C2", SortOrder = 5, CreatedAt = now.AddDays(-75) }
        );

        var mvpId       = Guid.NewGuid();
        var pitchId     = Guid.NewGuid();
        var networkId   = Guid.NewGuid();
        var gymId       = Guid.NewGuid();
        var runId       = Guid.NewGuid();
        var stretchId   = Guid.NewGuid();
        var engId       = Guid.NewGuid();
        var germanId    = Guid.NewGuid();
        var vocabId     = Guid.NewGuid();
        var designId    = Guid.NewGuid();
        var writeId     = Guid.NewGuid();
        var photoId     = Guid.NewGuid();
        var deepWorkId  = Guid.NewGuid();
        var planDayId   = Guid.NewGuid();
        var reviewId    = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = mvpId,      ActivityGroupId = startupGrp,      Title = "Build MVP feature",   DurationMinutes = 90, Description = "Ship one feature today", Icon = "🔧", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-88) },
            new ActivityItem { Id = pitchId,    ActivityGroupId = startupGrp,      Title = "Pitch practice",      DurationMinutes = 20, Description = "Record & review pitch", Icon = "🎤", Color = "#5C6BC0", SortOrder = 2, CreatedAt = now.AddDays(-85) },
            new ActivityItem { Id = networkId,  ActivityGroupId = startupGrp,      Title = "Networking",          DurationMinutes = 30, Description = "Reach out to 3 people", Icon = "🤝", Color = "#26C6DA", SortOrder = 3, CreatedAt = now.AddDays(-80) },
            new ActivityItem { Id = gymId,      ActivityGroupId = fitnessGrp,      Title = "Gym session",         DurationMinutes = 60, Description = "Push/Pull/Legs split", Icon = "🏋️", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-88) },
            new ActivityItem { Id = runId,      ActivityGroupId = fitnessGrp,      Title = "Morning run",         DurationMinutes = 30, Description = "5-7 km", Icon = "🏃", Color = "#4CAF50", SortOrder = 2, CreatedAt = now.AddDays(-85) },
            new ActivityItem { Id = stretchId,  ActivityGroupId = fitnessGrp,      Title = "Stretching",          DurationMinutes = 15, Icon = "🤸", Color = "#81C784", SortOrder = 3, CreatedAt = now.AddDays(-80) },
            new ActivityItem { Id = engId,      ActivityGroupId = languagesGrp,    Title = "English — speaking",  DurationMinutes = 30, Description = "iTalki session", Icon = "🇬🇧", Color = "#EF5350", SortOrder = 1, CreatedAt = now.AddDays(-85) },
            new ActivityItem { Id = germanId,   ActivityGroupId = languagesGrp,    Title = "German — Duolingo",   DurationMinutes = 15, Icon = "🇩🇪", Color = "#FFA726", SortOrder = 2, CreatedAt = now.AddDays(-80) },
            new ActivityItem { Id = vocabId,    ActivityGroupId = languagesGrp,    Title = "Anki flashcards",     DurationMinutes = 10, Description = "50 cards", Icon = "🃏", Color = "#FF7043", SortOrder = 3, TaskType = (int)ActivityItemType.Count, TargetCount = 50, CreatedAt = now.AddDays(-75) },
            new ActivityItem { Id = designId,   ActivityGroupId = creativeGrp,     Title = "UI design practice",  DurationMinutes = 45, Description = "Figma daily challenge", Icon = "🎨", Color = "#EC407A", SortOrder = 1, CreatedAt = now.AddDays(-80) },
            new ActivityItem { Id = writeId,    ActivityGroupId = creativeGrp,     Title = "Blog post",           DurationMinutes = 60, Description = "Write 500 words", Icon = "✏️", Color = "#AB47BC", SortOrder = 2, CreatedAt = now.AddDays(-75) },
            new ActivityItem { Id = photoId,    ActivityGroupId = creativeGrp,     Title = "Photo editing",       DurationMinutes = 30, Icon = "📸", Color = "#F06292", SortOrder = 3, CreatedAt = now.AddDays(-70) },
            new ActivityItem { Id = deepWorkId, ActivityGroupId = productivityGrp, Title = "Deep work session",   DurationMinutes = 90, Description = "No distractions", Icon = "🧠", Color = "#7E57C2", SortOrder = 1, CreatedAt = now.AddDays(-75) },
            new ActivityItem { Id = planDayId,  ActivityGroupId = productivityGrp, Title = "Plan tomorrow",       DurationMinutes = 15, Icon = "📋", Color = "#42A5F5", SortOrder = 2, CreatedAt = now.AddDays(-75) },
            new ActivityItem { Id = reviewId,   ActivityGroupId = productivityGrp, Title = "Weekly review",       DurationMinutes = 30, Description = "Reflect on progress", Icon = "📊", Color = "#26A69A", SortOrder = 3, CreatedAt = now.AddDays(-70) }
        );

        // ── Activity Completions (21 days) ───────────────────────────
        var proChances = new Dictionary<Guid, double>
        {
            [mvpId] = 0.90, [pitchId]   = 0.60, [networkId] = 0.50,
            [gymId] = 0.85, [runId]     = 0.75, [stretchId] = 0.80,
            [engId] = 0.70, [germanId]  = 0.65, [vocabId]   = 0.85,
            [designId] = 0.55, [writeId] = 0.40, [photoId]  = 0.45,
            [deepWorkId] = 0.90, [planDayId] = 0.95, [reviewId] = 0.30
        };
        SeedCompletions(context, VladPremiumId, proChances, 21, today, now, rng);

        // ── Daily Snapshots (60 days, aggressive improvement: 6h → 1.5h) ──
        SeedScreenTime(context, VladPremiumId, 60, [6.0, 5.2, 4.5, 3.5, 2.8, 2.2, 1.8, 1.5, 1.5], today, now, rng);

        // ── Achievements (5 unlocked — long streak) ──────────────────
        context.AccountAchievements.AddRange(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladPremiumId, AchievementId = AchStreak3,   UnlockedDate = today.AddDays(-80), CreatedAt = now.AddDays(-80) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladPremiumId, AchievementId = AchStreak7,   UnlockedDate = today.AddDays(-70), CreatedAt = now.AddDays(-70) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladPremiumId, AchievementId = AchStreak14,  UnlockedDate = today.AddDays(-55), CreatedAt = now.AddDays(-55) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladPremiumId, AchievementId = AchStreak30,  UnlockedDate = today.AddDays(-35), CreatedAt = now.AddDays(-35) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = VladPremiumId, AchievementId = AchStreak100, UnlockedDate = today.AddDays(-5),  CreatedAt = now.AddDays(-5) }
        );

        // ── Streak Freezes (2 used) ──────────────────────────────────
        context.StreakFreezes.AddRange(
            new StreakFreeze { Id = Guid.NewGuid(), AccountId = VladPremiumId, Date = today.AddDays(-22), CreatedAt = now.AddDays(-22) },
            new StreakFreeze { Id = Guid.NewGuid(), AccountId = VladPremiumId, Date = today.AddDays(-11), CreatedAt = now.AddDays(-11) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Vlad (Premium) seed complete: vladpro@gmail.com / VladPro123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  3. MIKA — fitness & sports
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedMikaAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == MikaId))
            return;

        logger?.LogInformation("Seeding Mika: mika@bloomdo.dev ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(101);

        context.Accounts.Add(new Account
        {
            Id = MikaId,
            Email = "mika@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Mika123!"),
            FirstName = "Mika",
            LastName = "Sergeev",
            Username = "mika_fit",
            Bio = "Gym rat 💪 Running marathons & eating clean",
            AvatarJson = MakeAvatar(2, 2, 1, 0, 1, 0, 0, 0, 1, 1, 0, 0, 3, 2, 3, 0, 0),
            ProfileVisibility = ProfileVisibility.Public,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-45)
        });

        context.AccountRoles.Add(new AccountRole { Id = Guid.NewGuid(), AccountId = MikaId, RoleId = (int)UserRole.User, CreatedAt = now });

        // Block Rules
        context.BlockRules.AddRange(
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = MikaId,
                Title = "Pre-workout focus", Type = BlockType.Focus, IsActive = true,
                FocusDurationMinutes = 60,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\",\"com.zhiliaoapp.musically\"]",
                CreatedAt = now.AddDays(-40)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = MikaId,
                Title = "Social limit", Type = BlockType.Limit, IsActive = true,
                DailyLimitMinutes = 60,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.twitter.android\"]",
                CreatedAt = now.AddDays(-35)
            }
        );

        // Activity Groups
        var workoutGrp   = Guid.NewGuid();
        var nutritionGrp = Guid.NewGuid();
        var recoveryGrp  = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = workoutGrp,   AccountId = MikaId, Title = "Workout",   Icon = "🏋️", Color = "#EF5350", SortOrder = 1, CreatedAt = now.AddDays(-43) },
            new ActivityGroup { Id = nutritionGrp, AccountId = MikaId, Title = "Nutrition",  Icon = "🥗", Color = "#66BB6A", SortOrder = 2, CreatedAt = now.AddDays(-43) },
            new ActivityGroup { Id = recoveryGrp,  AccountId = MikaId, Title = "Recovery",   Icon = "😴", Color = "#42A5F5", SortOrder = 3, CreatedAt = now.AddDays(-40) }
        );

        var pushPullId  = Guid.NewGuid();
        var cardioId    = Guid.NewGuid();
        var absId       = Guid.NewGuid();
        var mealPrepId  = Guid.NewGuid();
        var proteinId   = Guid.NewGuid();
        var sleepId     = Guid.NewGuid();
        var stretchMikaId = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = pushPullId,    ActivityGroupId = workoutGrp,   Title = "Push/Pull/Legs",    DurationMinutes = 60, Description = "Main workout", Icon = "🏋️", Color = "#EF5350", SortOrder = 1, CreatedAt = now.AddDays(-43) },
            new ActivityItem { Id = cardioId,      ActivityGroupId = workoutGrp,   Title = "Cardio 30 min",     DurationMinutes = 30, Description = "Run or bike", Icon = "🏃", Color = "#FF7043", SortOrder = 2, CreatedAt = now.AddDays(-43) },
            new ActivityItem { Id = absId,         ActivityGroupId = workoutGrp,   Title = "Ab workout",        DurationMinutes = 15, Icon = "💪", Color = "#FF5722", SortOrder = 3, CreatedAt = now.AddDays(-40) },
            new ActivityItem { Id = mealPrepId,    ActivityGroupId = nutritionGrp, Title = "Meal prep",         DurationMinutes = 45, Description = "Cook healthy meals", Icon = "🍳", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-43) },
            new ActivityItem { Id = proteinId,     ActivityGroupId = nutritionGrp, Title = "Hit protein goal",  Description = "150g+ protein", Icon = "🥩", Color = "#4CAF50", SortOrder = 2, TaskType = (int)ActivityItemType.Count, TargetCount = 150, CreatedAt = now.AddDays(-43) },
            new ActivityItem { Id = sleepId,       ActivityGroupId = recoveryGrp,  Title = "Sleep 8 hours",     Icon = "😴", Color = "#42A5F5", SortOrder = 1, TaskType = (int)ActivityItemType.Checkbox, CreatedAt = now.AddDays(-40) },
            new ActivityItem { Id = stretchMikaId, ActivityGroupId = recoveryGrp,  Title = "Foam rolling",      DurationMinutes = 15, Icon = "🧘", Color = "#29B6F6", SortOrder = 2, CreatedAt = now.AddDays(-38) }
        );

        var mikaChances = new Dictionary<Guid, double>
        {
            [pushPullId] = 0.85, [cardioId]  = 0.75, [absId]      = 0.60,
            [mealPrepId] = 0.65, [proteinId] = 0.90, [sleepId]    = 0.70,
            [stretchMikaId] = 0.55
        };
        SeedCompletions(context, MikaId, mikaChances, 12, today, now, rng);
        SeedScreenTime(context, MikaId, 30, [4.0, 3.5, 3.0, 2.5, 2.2], today, now, rng);

        context.AccountAchievements.AddRange(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = MikaId, AchievementId = AchStreak3, UnlockedDate = today.AddDays(-35), CreatedAt = now.AddDays(-35) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = MikaId, AchievementId = AchStreak7, UnlockedDate = today.AddDays(-25), CreatedAt = now.AddDays(-25) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Mika seed complete: mika@bloomdo.dev / Mika123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  4. DASHA — student & learning
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedDashaAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == DashaId))
            return;

        logger?.LogInformation("Seeding Dasha: dasha@bloomdo.dev ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(202);

        context.Accounts.Add(new Account
        {
            Id = DashaId,
            Email = "dasha@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dasha123!"),
            FirstName = "Dasha",
            LastName = "Orlova",
            Username = "dasha_study",
            Bio = "Med student 🩺 Learning never stops",
            AvatarJson = MakeAvatar(0, 0, 2, 1, 4, 3, 0, 0, 0, 0, 0, 0, 2, 4, 2, 1, 0),
            ProfileVisibility = ProfileVisibility.FriendsOnly,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-35)
        });

        context.AccountRoles.Add(new AccountRole { Id = Guid.NewGuid(), AccountId = DashaId, RoleId = (int)UserRole.User, CreatedAt = now });

        // Block Rules
        context.BlockRules.Add(new BlockRule
        {
            Id = Guid.NewGuid(), AccountId = DashaId,
            Title = "Study hours", Type = BlockType.Schedule, IsActive = true,
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(14, 0),
            BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\",\"com.google.android.youtube\"]",
            ScheduleDaysJson = "[1,2,3,4,5]",
            CreatedAt = now.AddDays(-30)
        });

        // Activity Groups
        var universityGrp = Guid.NewGuid();
        var langGrp       = Guid.NewGuid();
        var hobbyGrp      = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = universityGrp, AccountId = DashaId, Title = "University",  Icon = "🎓", Color = "#5C6BC0", SortOrder = 1, CreatedAt = now.AddDays(-33) },
            new ActivityGroup { Id = langGrp,       AccountId = DashaId, Title = "Languages",   Icon = "🇬🇧", Color = "#EF5350", SortOrder = 2, CreatedAt = now.AddDays(-33) },
            new ActivityGroup { Id = hobbyGrp,      AccountId = DashaId, Title = "Hobbies",     Icon = "🎨", Color = "#FF9800", SortOrder = 3, CreatedAt = now.AddDays(-30) }
        );

        var anatomyId   = Guid.NewGuid();
        var flashcardsId = Guid.NewGuid();
        var essayId     = Guid.NewGuid();
        var ieltsId     = Guid.NewGuid();
        var readDashaId = Guid.NewGuid();
        var drawId      = Guid.NewGuid();
        var pianoId     = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = anatomyId,    ActivityGroupId = universityGrp, Title = "Anatomy review",  DurationMinutes = 60, Description = "Lecture notes + atlas", Icon = "🩺", Color = "#5C6BC0", SortOrder = 1, CreatedAt = now.AddDays(-33) },
            new ActivityItem { Id = flashcardsId, ActivityGroupId = universityGrp, Title = "Anki med cards",  DurationMinutes = 20, Description = "100 flashcards", Icon = "🃏", Color = "#7E57C2", SortOrder = 2, TaskType = (int)ActivityItemType.Count, TargetCount = 100, CreatedAt = now.AddDays(-33) },
            new ActivityItem { Id = essayId,      ActivityGroupId = universityGrp, Title = "Write essay",     DurationMinutes = 45, Description = "Research paper progress", Icon = "📝", Color = "#42A5F5", SortOrder = 3, CreatedAt = now.AddDays(-30) },
            new ActivityItem { Id = ieltsId,      ActivityGroupId = langGrp,       Title = "IELTS practice",  DurationMinutes = 40, Description = "Listening & writing", Icon = "🇬🇧", Color = "#EF5350", SortOrder = 1, CreatedAt = now.AddDays(-33) },
            new ActivityItem { Id = readDashaId,  ActivityGroupId = langGrp,       Title = "Read in English",  DurationMinutes = 25, Description = "Fiction or articles", Icon = "📖", Color = "#FF5722", SortOrder = 2, CreatedAt = now.AddDays(-30) },
            new ActivityItem { Id = drawId,       ActivityGroupId = hobbyGrp,      Title = "Sketch 30 min",    DurationMinutes = 30, Description = "Urban sketching", Icon = "🎨", Color = "#FF9800", SortOrder = 1, CreatedAt = now.AddDays(-30) },
            new ActivityItem { Id = pianoId,      ActivityGroupId = hobbyGrp,      Title = "Piano practice",   DurationMinutes = 20, Icon = "🎹", Color = "#AB47BC", SortOrder = 2, CreatedAt = now.AddDays(-28) }
        );

        var dashaChances = new Dictionary<Guid, double>
        {
            [anatomyId] = 0.85, [flashcardsId] = 0.90, [essayId]  = 0.50,
            [ieltsId]   = 0.70, [readDashaId]  = 0.75, [drawId]   = 0.45,
            [pianoId]   = 0.40
        };
        SeedCompletions(context, DashaId, dashaChances, 10, today, now, rng);
        SeedScreenTime(context, DashaId, 25, [5.0, 4.2, 3.5, 3.0], today, now, rng);

        context.AccountAchievements.AddRange(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = DashaId, AchievementId = AchStreak3, UnlockedDate = today.AddDays(-20), CreatedAt = now.AddDays(-20) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = DashaId, AchievementId = AchStreak7, UnlockedDate = today.AddDays(-10), CreatedAt = now.AddDays(-10) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Dasha seed complete: dasha@bloomdo.dev / Dasha123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  5. ARTEM — programmer & productivity
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedArtemAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == ArtemId))
            return;

        logger?.LogInformation("Seeding Artem: artem@bloomdo.dev ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(303);

        context.Accounts.Add(new Account
        {
            Id = ArtemId,
            Email = "artem@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Artem123!"),
            FirstName = "Artem",
            LastName = "Bykov",
            Username = "artem_dev",
            Bio = "Full-stack dev 🖥️ Open source contributor",
            AvatarJson = MakeAvatar(3, 2, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1),
            ProfileVisibility = ProfileVisibility.Public,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-50)
        });

        context.AccountRoles.Add(new AccountRole { Id = Guid.NewGuid(), AccountId = ArtemId, RoleId = (int)UserRole.User, CreatedAt = now });

        // Block Rules
        context.BlockRules.AddRange(
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = ArtemId,
                Title = "Coding time", Type = BlockType.Focus, IsActive = true,
                FocusDurationMinutes = 120,
                BlockedPackagesJson = "[\"com.instagram.android\",\"com.reddit.frontpage\",\"com.twitter.android\"]",
                CreatedAt = now.AddDays(-48)
            },
            new BlockRule
            {
                Id = Guid.NewGuid(), AccountId = ArtemId,
                Title = "Reddit limit", Type = BlockType.Limit, IsActive = true,
                DailyLimitMinutes = 30,
                BlockedPackagesJson = "[\"com.reddit.frontpage\"]",
                CreatedAt = now.AddDays(-45)
            }
        );

        // Activity Groups
        var devGrp    = Guid.NewGuid();
        var ossGrp    = Guid.NewGuid();
        var careerGrp = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = devGrp,    AccountId = ArtemId, Title = "Dev",          Icon = "💻", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-48) },
            new ActivityGroup { Id = ossGrp,    AccountId = ArtemId, Title = "Open Source",  Icon = "🌐", Color = "#66BB6A", SortOrder = 2, CreatedAt = now.AddDays(-45) },
            new ActivityGroup { Id = careerGrp, AccountId = ArtemId, Title = "Career",       Icon = "📈", Color = "#FF9800", SortOrder = 3, CreatedAt = now.AddDays(-40) }
        );

        var leetArtemId = Guid.NewGuid();
        var buildId     = Guid.NewGuid();
        var testId      = Guid.NewGuid();
        var prId        = Guid.NewGuid();
        var docsId      = Guid.NewGuid();
        var interviewId = Guid.NewGuid();
        var linkedinId  = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = leetArtemId, ActivityGroupId = devGrp,    Title = "LeetCode",         DurationMinutes = 45, Description = "Medium+ difficulty", Icon = "🧩", Color = "#42A5F5", SortOrder = 1, CreatedAt = now.AddDays(-48) },
            new ActivityItem { Id = buildId,     ActivityGroupId = devGrp,    Title = "Personal project",  DurationMinutes = 90, Description = "Build & ship", Icon = "🛠️", Color = "#5C6BC0", SortOrder = 2, CreatedAt = now.AddDays(-48) },
            new ActivityItem { Id = testId,      ActivityGroupId = devGrp,    Title = "Write tests",       DurationMinutes = 30, Description = "Unit & integration", Icon = "✅", Color = "#26C6DA", SortOrder = 3, CreatedAt = now.AddDays(-45) },
            new ActivityItem { Id = prId,        ActivityGroupId = ossGrp,    Title = "Review PR",         DurationMinutes = 20, Icon = "🔍", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-45) },
            new ActivityItem { Id = docsId,      ActivityGroupId = ossGrp,    Title = "Write docs",        DurationMinutes = 30, Description = "README / wiki", Icon = "📄", Color = "#4CAF50", SortOrder = 2, CreatedAt = now.AddDays(-42) },
            new ActivityItem { Id = interviewId, ActivityGroupId = careerGrp, Title = "Interview prep",    DurationMinutes = 45, Description = "System design + behavioral", Icon = "🎯", Color = "#FF9800", SortOrder = 1, CreatedAt = now.AddDays(-40) },
            new ActivityItem { Id = linkedinId,  ActivityGroupId = careerGrp, Title = "LinkedIn post",     DurationMinutes = 15, Icon = "💼", Color = "#0077B5", SortOrder = 2, CreatedAt = now.AddDays(-38) }
        );

        var artemChances = new Dictionary<Guid, double>
        {
            [leetArtemId] = 0.80, [buildId]     = 0.75, [testId]    = 0.60,
            [prId]        = 0.55, [docsId]      = 0.40, [interviewId] = 0.65,
            [linkedinId]  = 0.30
        };
        SeedCompletions(context, ArtemId, artemChances, 14, today, now, rng);
        SeedScreenTime(context, ArtemId, 35, [4.5, 4.0, 3.5, 2.8, 2.3, 2.0], today, now, rng);

        context.AccountAchievements.AddRange(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = ArtemId, AchievementId = AchStreak3,  UnlockedDate = today.AddDays(-40), CreatedAt = now.AddDays(-40) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = ArtemId, AchievementId = AchStreak7,  UnlockedDate = today.AddDays(-30), CreatedAt = now.AddDays(-30) },
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = ArtemId, AchievementId = AchStreak14, UnlockedDate = today.AddDays(-15), CreatedAt = now.AddDays(-15) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Artem seed complete: artem@bloomdo.dev / Artem123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  6. LENA — creative & art
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedLenaAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == LenaId))
            return;

        logger?.LogInformation("Seeding Lena: lena@bloomdo.dev ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(404);

        context.Accounts.Add(new Account
        {
            Id = LenaId,
            Email = "lena@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lena123!"),
            FirstName = "Lena",
            LastName = "Titova",
            Username = "lena_art",
            Bio = "Artist & musician 🎨🎵 Creating every day",
            AvatarJson = MakeAvatar(0, 0, 4, 1, 3, 4, 0, 0, 0, 0, 0, 0, 0, 5, 5, 2, 0),
            ProfileVisibility = ProfileVisibility.Public,
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-28)
        });

        context.AccountRoles.Add(new AccountRole { Id = Guid.NewGuid(), AccountId = LenaId, RoleId = (int)UserRole.User, CreatedAt = now });

        // Block Rules
        context.BlockRules.Add(new BlockRule
        {
            Id = Guid.NewGuid(), AccountId = LenaId,
            Title = "Creative time", Type = BlockType.Schedule, IsActive = true,
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(13, 0),
            BlockedPackagesJson = "[\"com.instagram.android\",\"com.zhiliaoapp.musically\"]",
            ScheduleDaysJson = "[0,1,2,3,4,5,6]",
            CreatedAt = now.AddDays(-25)
        });

        // Activity Groups
        var artGrp     = Guid.NewGuid();
        var musicGrp   = Guid.NewGuid();
        var wellnessGrp = Guid.NewGuid();

        context.ActivityGroups.AddRange(
            new ActivityGroup { Id = artGrp,      AccountId = LenaId, Title = "Art",      Icon = "🎨", Color = "#EC407A", SortOrder = 1, CreatedAt = now.AddDays(-26) },
            new ActivityGroup { Id = musicGrp,    AccountId = LenaId, Title = "Music",    Icon = "🎵", Color = "#AB47BC", SortOrder = 2, CreatedAt = now.AddDays(-26) },
            new ActivityGroup { Id = wellnessGrp, AccountId = LenaId, Title = "Wellness", Icon = "🌸", Color = "#66BB6A", SortOrder = 3, CreatedAt = now.AddDays(-24) }
        );

        var paintId     = Guid.NewGuid();
        var sketchLenaId = Guid.NewGuid();
        var digitalId   = Guid.NewGuid();
        var guitarId    = Guid.NewGuid();
        var singId      = Guid.NewGuid();
        var yogaId      = Guid.NewGuid();
        var gratitudeId = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = paintId,      ActivityGroupId = artGrp,      Title = "Watercolor session",  DurationMinutes = 60, Description = "Landscape or portrait", Icon = "🖌️", Color = "#EC407A", SortOrder = 1, CreatedAt = now.AddDays(-26) },
            new ActivityItem { Id = sketchLenaId, ActivityGroupId = artGrp,      Title = "Daily sketch",        DurationMinutes = 20, Description = "Quick gesture drawings", Icon = "✏️", Color = "#F06292", SortOrder = 2, CreatedAt = now.AddDays(-26) },
            new ActivityItem { Id = digitalId,    ActivityGroupId = artGrp,      Title = "Digital art",          DurationMinutes = 45, Description = "Procreate practice", Icon = "🖥️", Color = "#E91E63", SortOrder = 3, CreatedAt = now.AddDays(-24) },
            new ActivityItem { Id = guitarId,     ActivityGroupId = musicGrp,    Title = "Guitar practice",      DurationMinutes = 30, Description = "Chord progressions", Icon = "🎸", Color = "#AB47BC", SortOrder = 1, CreatedAt = now.AddDays(-26) },
            new ActivityItem { Id = singId,       ActivityGroupId = musicGrp,    Title = "Vocal warmup",         DurationMinutes = 15, Icon = "🎤", Color = "#9C27B0", SortOrder = 2, CreatedAt = now.AddDays(-24) },
            new ActivityItem { Id = yogaId,       ActivityGroupId = wellnessGrp, Title = "Morning yoga",         DurationMinutes = 20, Icon = "🧘", Color = "#66BB6A", SortOrder = 1, CreatedAt = now.AddDays(-24) },
            new ActivityItem { Id = gratitudeId,  ActivityGroupId = wellnessGrp, Title = "Gratitude journal",    DurationMinutes = 10, Description = "3 things I'm grateful for", Icon = "💕", Color = "#4CAF50", SortOrder = 2, CreatedAt = now.AddDays(-22) }
        );

        var lenaChances = new Dictionary<Guid, double>
        {
            [paintId] = 0.55, [sketchLenaId] = 0.80, [digitalId]  = 0.45,
            [guitarId] = 0.70, [singId]      = 0.60, [yogaId]     = 0.75,
            [gratitudeId] = 0.85
        };
        SeedCompletions(context, LenaId, lenaChances, 10, today, now, rng);
        SeedScreenTime(context, LenaId, 20, [3.5, 3.0, 2.5, 2.2], today, now, rng);

        context.AccountAchievements.Add(
            new AccountAchievement { Id = Guid.NewGuid(), AccountId = LenaId, AchievementId = AchStreak3, UnlockedDate = today.AddDays(-15), CreatedAt = now.AddDays(-15) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Lena seed complete: lena@bloomdo.dev / Lena123!");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  7-16. EXTRA ACCOUNTS (10 filler accounts for friend lists)
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedExtraAccountsAsync(AppDbContext context, ILogger? logger)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == NikitaId))
            return;

        logger?.LogInformation("Seeding 10 extra accounts ...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rng = new Random(500);

        var extras = new[]
        {
            (Id: NikitaId, Email: "nikita@bloomdo.dev", Pass: "Nikita123!", First: "Nikita", Last: "Volkov",    User: "nikita_v",    Bio: "Hiker & photographer 🏔️",         Vis: ProfileVisibility.Public,      Days: 40, Av: MakeAvatar(2, 2, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0)),
            (Id: SonyaId,  Email: "sonya@bloomdo.dev",  Pass: "Sonya123!",  First: "Sonya",  Last: "Ivanova",   User: "sonya_i",     Bio: "Yoga & mindfulness 🧘‍♀️",           Vis: ProfileVisibility.FriendsOnly, Days: 35, Av: MakeAvatar(0, 0, 3, 1, 2, 2, 0, 0, 0, 0, 0, 0, 1, 3, 1, 1, 0)),
            (Id: MaxId,    Email: "max@bloomdo.dev",    Pass: "Max123!",    First: "Max",    Last: "Petrov",    User: "max_p",       Bio: "Gamer turning productive 🎮→📚",   Vis: ProfileVisibility.Public,      Days: 25, Av: MakeAvatar(1, 1, 1, 0, 0, 0, 1, 2, 0, 0, 1, 1, 2, 0, 3, 0, 0)),
            (Id: KatyaId,  Email: "katya@bloomdo.dev",  Pass: "Katya123!",  First: "Katya",  Last: "Smirnova", User: "katya_s",     Bio: "Book lover & writer 📚✍️",          Vis: ProfileVisibility.FriendsOnly, Days: 30, Av: MakeAvatar(1, 0, 4, 1, 3, 3, 0, 0, 0, 0, 0, 0, 0, 2, 4, 2, 0)),
            (Id: RomaId,   Email: "roma@bloomdo.dev",   Pass: "Roma123!",   First: "Roma",   Last: "Kuznetsov", User: "roma_k",     Bio: "Basketball & chill 🏀",             Vis: ProfileVisibility.Public,      Days: 20, Av: MakeAvatar(3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1, 2, 0, 0)),
            (Id: NastyaId, Email: "nastya@bloomdo.dev", Pass: "Nastya123!", First: "Nastya", Last: "Popova",   User: "nastya_p",    Bio: "Cooking healthy meals 🍳🥑",        Vis: ProfileVisibility.Public,      Days: 18, Av: MakeAvatar(0, 0, 2, 0, 4, 4, 0, 0, 0, 0, 0, 0, 2, 4, 3, 0, 0)),
            (Id: IgorId,   Email: "igor@bloomdo.dev",   Pass: "Igor123!",   First: "Igor",   Last: "Novikov",  User: "igor_n",      Bio: "Running towards goals 🏃‍♂️",         Vis: ProfileVisibility.Private,     Days: 15, Av: MakeAvatar(2, 2, 0, 0, 1, 0, 0, 0, 2, 2, 0, 0, 1, 0, 1, 0, 2)),
            (Id: PolinaId, Email: "polina@bloomdo.dev", Pass: "Polina123!", First: "Polina", Last: "Morozova", User: "polina_m",    Bio: "Design student 🎨",                 Vis: ProfileVisibility.FriendsOnly, Days: 22, Av: MakeAvatar(0, 0, 3, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 5, 5, 1, 0)),
            (Id: DimaId,   Email: "dima@bloomdo.dev",   Pass: "Dima123!",   First: "Dima",   Last: "Sokolov",  User: "dima_s",      Bio: "Chess & coding ♟️💻",                Vis: ProfileVisibility.Public,      Days: 28, Av: MakeAvatar(1, 1, 0, 0, 3, 1, 1, 0, 0, 0, 0, 0, 1, 2, 0, 0, 0)),
            (Id: AnyaId,   Email: "anya@bloomdo.dev",   Pass: "Anya123!",   First: "Anya",   Last: "Fedorova", User: "anya_f",      Bio: "Plant mom & journaling 🌿📓",       Vis: ProfileVisibility.Public,      Days: 12, Av: MakeAvatar(0, 0, 1, 0, 0, 3, 0, 0, 0, 0, 0, 0, 2, 3, 2, 0, 0))
        };

        // Groups + Items for each extra (one group, 2 items each to keep it lightweight)
        var groupTemplates = new[]
        {
            (Title: "Outdoors",     Icon: "🏔️", Color: "#4CAF50", Items: new[] { ("Hike 1 hour", "🥾", 60, 0, 0), ("Photo walk", "📷", 30, 0, 0) }),
            (Title: "Mindfulness",  Icon: "🧘", Color: "#AB47BC", Items: new[] { ("Meditate", "🧘", 15, 0, 0), ("Breathwork", "🌬️", 10, 0, 0) }),
            (Title: "Gaming detox", Icon: "🎮", Color: "#EF5350", Items: new[] { ("No gaming 2h", "⏱️", 120, 0, 0), ("Read instead", "📖", 30, 0, 0) }),
            (Title: "Reading",      Icon: "📚", Color: "#FF9800", Items: new[] { ("Read 30 min", "📖", 30, 0, 0), ("Write notes", "📝", 15, 0, 0) }),
            (Title: "Sports",       Icon: "🏀", Color: "#FF5722", Items: new[] { ("Basketball", "🏀", 60, 0, 0), ("Stretching", "🤸", 15, 0, 0) }),
            (Title: "Cooking",      Icon: "🍳", Color: "#66BB6A", Items: new[] { ("New recipe", "👨‍🍳", 45, 0, 0), ("Meal prep", "🥗", 30, 0, 0) }),
            (Title: "Running",      Icon: "🏃", Color: "#42A5F5", Items: new[] { ("5k run", "🏃", 30, 0, 0), ("Stretching", "🤸", 10, 0, 0) }),
            (Title: "Design",       Icon: "🎨", Color: "#EC407A", Items: new[] { ("Figma practice", "🖥️", 45, 0, 0), ("Color theory", "🌈", 20, 0, 0) }),
            (Title: "Chess & Logic", Icon: "♟️", Color: "#5C6BC0", Items: new[] { ("Chess puzzles", "♟️", 30, 0, 0), ("Code kata", "🧩", 45, 0, 0) }),
            (Title: "Journaling",   Icon: "📓", Color: "#26A69A", Items: new[] { ("Morning pages", "📝", 20, 0, 0), ("Gratitude list", "💕", 10, 0, 0) })
        };

        for (var i = 0; i < extras.Length; i++)
        {
            var e = extras[i];
            var gt = groupTemplates[i];

            context.Accounts.Add(new Account
            {
                Id = e.Id,
                Email = e.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(e.Pass),
                FirstName = e.First,
                LastName = e.Last,
                Username = e.User,
                Bio = e.Bio,
                AvatarJson = e.Av,
                ProfileVisibility = e.Vis,
                IsEmailConfirmed = true,
                LastLoginAt = now.AddDays(-rng.Next(0, 3)),
                CreatedAt = now.AddDays(-e.Days)
            });

            context.AccountRoles.Add(new AccountRole
            {
                Id = Guid.NewGuid(), AccountId = e.Id,
                RoleId = (int)UserRole.User, CreatedAt = now
            });

            // One block rule for some accounts
            if (i % 2 == 0)
            {
                context.BlockRules.Add(new BlockRule
                {
                    Id = Guid.NewGuid(), AccountId = e.Id,
                    Title = "Screen time limit", Type = BlockType.Limit, IsActive = true,
                    DailyLimitMinutes = 45 + rng.Next(0, 30),
                    BlockedPackagesJson = "[\"com.instagram.android\",\"com.google.android.youtube\"]",
                    CreatedAt = now.AddDays(-e.Days + 2)
                });
            }

            // Activity group + items
            var grpId = Guid.NewGuid();
            context.ActivityGroups.Add(new ActivityGroup
            {
                Id = grpId, AccountId = e.Id,
                Title = gt.Title, Icon = gt.Icon, Color = gt.Color,
                SortOrder = 1, CreatedAt = now.AddDays(-e.Days + 1)
            });

            var itemIds = new List<Guid>();
            for (var j = 0; j < gt.Items.Length; j++)
            {
                var item = gt.Items[j];
                var itemId = Guid.NewGuid();
                itemIds.Add(itemId);
                context.ActivityItems.Add(new ActivityItem
                {
                    Id = itemId, ActivityGroupId = grpId,
                    Title = item.Item1, Icon = item.Item2, DurationMinutes = item.Item3,
                    Color = gt.Color, SortOrder = j + 1,
                    CreatedAt = now.AddDays(-e.Days + 1)
                });
            }

            // Completions (5-8 days)
            var completionDays = 5 + rng.Next(0, 4);
            var chances = itemIds.ToDictionary(id => id, _ => 0.5 + rng.NextDouble() * 0.35);
            SeedCompletions(context, e.Id, chances, completionDays, today, now, rng);

            // Screen time (shorter history)
            SeedScreenTime(context, e.Id, Math.Min(e.Days, 14), [4.5, 3.8, 3.2, 2.8], today, now, rng);

            // Achievement for the more active ones
            if (e.Days > 20)
            {
                context.AccountAchievements.Add(
                    new AccountAchievement { Id = Guid.NewGuid(), AccountId = e.Id, AchievementId = AchStreak3, UnlockedDate = today.AddDays(-e.Days + 10), CreatedAt = now.AddDays(-e.Days + 10) }
                );
            }
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Extra accounts seed complete (10 accounts)");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  FRIENDSHIPS
    // ═════════════════════════════════════════════════════════════════════

    private static async Task SeedFriendshipsAsync(AppDbContext context, ILogger? logger)
    {
        // Sentinel: check if VladFree→Mika friendship already exists
        if (await context.Friendships.IgnoreQueryFilters()
                .AnyAsync(f => f.RequesterId == VladFreeId && f.AddresseeId == MikaId))
            return;

        logger?.LogInformation("Seeding friendship graph ...");

        var now = DateTime.UtcNow;

        void Mutual(Guid a, Guid b, int daysAgo)
        {
            context.Friendships.AddRange(
                new Friendship { Id = Guid.NewGuid(), RequesterId = a, AddresseeId = b, Status = FriendshipStatus.Accepted, CreatedAt = now.AddDays(-daysAgo) },
                new Friendship { Id = Guid.NewGuid(), RequesterId = b, AddresseeId = a, Status = FriendshipStatus.Accepted, CreatedAt = now.AddDays(-daysAgo + 1) }
            );
        }

        void Pending(Guid requester, Guid addressee, int daysAgo)
        {
            context.Friendships.Add(
                new Friendship { Id = Guid.NewGuid(), RequesterId = requester, AddresseeId = addressee, Status = FriendshipStatus.Pending, CreatedAt = now.AddDays(-daysAgo) }
            );
        }

        // ── Vlad Free — close friends ────────────────────────────────
        Mutual(VladFreeId, MikaId,   30);
        Mutual(VladFreeId, DashaId,  28);
        Mutual(VladFreeId, ArtemId,  25);
        Mutual(VladFreeId, LenaId,   22);

        // ── Vlad Free — extra friends ────────────────────────────────
        Mutual(VladFreeId, NikitaId, 18);
        Mutual(VladFreeId, SonyaId,  15);
        Mutual(VladFreeId, MaxId,    12);

        // ── Vlad Free — pending ──────────────────────────────────────
        Pending(IgorId, VladFreeId, 3);

        // ── Vlad Premium — close friends ─────────────────────────────
        Mutual(VladPremiumId, MikaId,  35);
        Mutual(VladPremiumId, ArtemId, 33);
        Mutual(VladPremiumId, DashaId, 30);
        Mutual(VladPremiumId, LenaId,  28);

        // ── Vlad Premium — extra friends ─────────────────────────────
        Mutual(VladPremiumId, KatyaId,  20);
        Mutual(VladPremiumId, RomaId,   18);
        Mutual(VladPremiumId, NastyaId, 15);

        // ── Vlad Premium — pending ───────────────────────────────────
        Pending(PolinaId, VladPremiumId, 2);

        // ── Cross-friendships between friends ────────────────────────
        Mutual(MikaId,   ArtemId, 20);
        Mutual(MikaId,   DashaId, 18);
        Mutual(DashaId,  LenaId,  15);
        Mutual(NikitaId, MaxId,   10);
        Mutual(NikitaId, RomaId,  8);
        Mutual(SonyaId,  KatyaId, 12);
        Mutual(SonyaId,  AnyaId,  10);
        Mutual(DimaId,   IgorId,  7);

        // ── Shared Group (VladFree + Mika + Artem) ───────────────────
        var sharedGroupId = Guid.NewGuid();
        context.ActivityGroups.Add(new ActivityGroup
        {
            Id = sharedGroupId, AccountId = VladFreeId,
            Title = "Gym Buddies", Icon = "💪", Color = "#EF5350",
            SortOrder = 10, CreatedAt = now.AddDays(-20)
        });

        context.GroupMemberships.AddRange(
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = sharedGroupId, AccountId = VladFreeId, Role = GroupMemberRole.Owner,  Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-20) },
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = sharedGroupId, AccountId = MikaId,     Role = GroupMemberRole.Member, Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-19) },
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = sharedGroupId, AccountId = ArtemId,    Role = GroupMemberRole.Member, Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-18) }
        );

        var pushupId = Guid.NewGuid();
        var squatId  = Guid.NewGuid();
        var plankId  = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = pushupId, ActivityGroupId = sharedGroupId, Title = "50 Pushups",     TaskType = (int)ActivityItemType.Count, TargetCount = 50, Icon = "👊", Color = "#EF5350", SortOrder = 1, CreatedAt = now.AddDays(-20) },
            new ActivityItem { Id = squatId,  ActivityGroupId = sharedGroupId, Title = "30 Squats",      TaskType = (int)ActivityItemType.Count, TargetCount = 30, Icon = "🦵", Color = "#FF9800", SortOrder = 2, CreatedAt = now.AddDays(-20) },
            new ActivityItem { Id = plankId,  ActivityGroupId = sharedGroupId, Title = "2 min Plank",    DurationMinutes = 2,                                      Icon = "🧱", Color = "#42A5F5", SortOrder = 3, CreatedAt = now.AddDays(-20) }
        );

        // Completions in shared group (today & yesterday)
        var today = DateOnly.FromDateTime(now);
        context.ActivityCompletions.AddRange(
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = pushupId, AccountId = VladFreeId, Date = today, CountValue = 50, CompletedAtUtc = now.AddHours(-2), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = squatId,  AccountId = MikaId,     Date = today, CountValue = 30, CompletedAtUtc = now.AddHours(-3), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = plankId,  AccountId = ArtemId,    Date = today, CompletedAtUtc = now.AddHours(-1), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = pushupId, AccountId = MikaId,     Date = today.AddDays(-1), CountValue = 50, CompletedAtUtc = now.AddDays(-1), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = pushupId, AccountId = ArtemId,    Date = today.AddDays(-1), CountValue = 50, CompletedAtUtc = now.AddDays(-1), CreatedAt = now }
        );

        // ── Shared Group (VladPremium + Dasha + Lena) ────────────────
        var studyGroupId = Guid.NewGuid();
        context.ActivityGroups.Add(new ActivityGroup
        {
            Id = studyGroupId, AccountId = VladPremiumId,
            Title = "Study Squad", Icon = "📚", Color = "#5C6BC0",
            SortOrder = 10, CreatedAt = now.AddDays(-15)
        });

        context.GroupMemberships.AddRange(
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = studyGroupId, AccountId = VladPremiumId, Role = GroupMemberRole.Owner,  Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-15) },
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = studyGroupId, AccountId = DashaId,       Role = GroupMemberRole.Member, Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-14) },
            new GroupMembership { Id = Guid.NewGuid(), ActivityGroupId = studyGroupId, AccountId = LenaId,        Role = GroupMemberRole.Member, Status = GroupMemberStatus.Accepted, CreatedAt = now.AddDays(-13) }
        );

        var focusReadId = Guid.NewGuid();
        var noPhoneId   = Guid.NewGuid();

        context.ActivityItems.AddRange(
            new ActivityItem { Id = focusReadId, ActivityGroupId = studyGroupId, Title = "Focus reading 1h",  DurationMinutes = 60, Icon = "📖", Color = "#5C6BC0", SortOrder = 1, CreatedAt = now.AddDays(-15) },
            new ActivityItem { Id = noPhoneId,   ActivityGroupId = studyGroupId, Title = "No phone for 2h",   DurationMinutes = 120, Icon = "📵", Color = "#EF5350", SortOrder = 2, CreatedAt = now.AddDays(-15) }
        );

        context.ActivityCompletions.AddRange(
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = focusReadId, AccountId = VladPremiumId, Date = today, CompletedAtUtc = now.AddHours(-4), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = noPhoneId,   AccountId = DashaId,       Date = today, CompletedAtUtc = now.AddHours(-5), CreatedAt = now },
            new ActivityCompletion { Id = Guid.NewGuid(), ActivityItemId = focusReadId, AccountId = LenaId,        Date = today.AddDays(-1), CompletedAtUtc = now.AddDays(-1), CreatedAt = now }
        );

        // ── Notifications (samples) ─────────────────────────────────
        context.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), RecipientId = VladFreeId, ActorId = IgorId,   Type = NotificationType.FollowRequest,       IsRead = false, CreatedAt = now.AddDays(-3) },
            new Notification { Id = Guid.NewGuid(), RecipientId = VladFreeId, ActorId = MikaId,   Type = NotificationType.GroupTaskCompleted,   ReferenceId = sharedGroupId, IsRead = false, CreatedAt = now.AddHours(-3) },
            new Notification { Id = Guid.NewGuid(), RecipientId = VladPremiumId, ActorId = PolinaId, Type = NotificationType.FollowRequest,     IsRead = false, CreatedAt = now.AddDays(-2) },
            new Notification { Id = Guid.NewGuid(), RecipientId = VladPremiumId, ActorId = DashaId, Type = NotificationType.GroupTaskCompleted,  ReferenceId = studyGroupId, IsRead = true, CreatedAt = now.AddHours(-5) },
            new Notification { Id = Guid.NewGuid(), RecipientId = MikaId,  ActorId = VladFreeId,    Type = NotificationType.NewFollower,         IsRead = true, CreatedAt = now.AddDays(-30) },
            new Notification { Id = Guid.NewGuid(), RecipientId = ArtemId, ActorId = VladPremiumId,  Type = NotificationType.NewFollower,         IsRead = true, CreatedAt = now.AddDays(-33) }
        );

        await context.SaveChangesAsync();
        logger?.LogInformation("Friendship graph & shared groups seed complete.");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPER METHODS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates a JSON avatar configuration string.
    /// </summary>
    private static string MakeAvatar(
        int skinTone, int bodyType, int eyeColor, int eyeStyle,
        int hairColor, int hairStyle, int glassesStyle, int glassesColor,
        int facialHair, int facialHairColor, int headwearStyle, int headwearColor,
        int clothingStyle, int clothingColor, int backgroundColor,
        int mouthStyle, int faceExtra)
    {
        return JsonSerializer.Serialize(new
        {
            SkinTone = skinTone,
            BodyType = bodyType,
            EyeColor = eyeColor,
            EyeStyle = eyeStyle,
            HairColor = hairColor,
            HairStyle = hairStyle,
            GlassesStyle = glassesStyle,
            GlassesColor = glassesColor,
            FacialHair = facialHair,
            FacialHairColor = facialHairColor,
            HeadwearStyle = headwearStyle,
            HeadwearColor = headwearColor,
            ClothingStyle = clothingStyle,
            ClothingColor = clothingColor,
            BackgroundColor = backgroundColor,
            MouthStyle = mouthStyle,
            FaceExtra = faceExtra
        });
    }

    /// <summary>
    /// Seeds daily snapshots + per-app usage records over <paramref name="totalDays"/>.
    /// </summary>
    private static void SeedScreenTime(
        AppDbContext context, Guid accountId, int totalDays,
        double[] weeklyBaselines, DateOnly today, DateTime now, Random rng)
    {
        for (var dayOffset = totalDays - 1; dayOffset >= 0; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            var weekIndex = dayOffset / 7;
            var reversedWeek = weeklyBaselines.Length - 1 - Math.Min(weekIndex, weeklyBaselines.Length - 1);

            var baselineHours = weeklyBaselines[reversedWeek];
            var variation = 0.75 + rng.NextDouble() * 0.5;
            var totalHours = baselineHours * variation;
            var totalSeconds = (int)(totalHours * 3600);
            var goalMet = totalHours < 3.5;
            var pickups = Math.Max(1, (int)(totalHours * 7 + rng.Next(-3, 10)));

            context.DailySnapshots.Add(new DailySnapshot
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = pickups,
                GoalMet = goalMet,
                CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
            });

            var remainingSeconds = totalSeconds;
            var appsForDay = AllApps.OrderBy(_ => rng.Next()).Take(rng.Next(3, 7)).ToArray();

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
                    appSeconds = (int)(remainingSeconds * share * (0.3 + rng.NextDouble() * 0.4));
                    appSeconds = Math.Min(appSeconds, remainingSeconds);
                }

                if (appSeconds <= 0) continue;
                remainingSeconds -= appSeconds;

                context.AppUsageRecords.Add(new AppUsageRecord
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    Date = date,
                    PackageName = appsForDay[a].Package,
                    AppLabel = appsForDay[a].Label,
                    ForegroundSeconds = appSeconds,
                    CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                });
            }
        }
    }

    /// <summary>
    /// Seeds activity completions for <paramref name="days"/> days going backwards from today.
    /// </summary>
    private static void SeedCompletions(
        AppDbContext context, Guid accountId,
        Dictionary<Guid, double> itemChances, int days,
        DateOnly today, DateTime now, Random rng)
    {
        for (var dayOffset = 0; dayOffset < days; dayOffset++)
        {
            var date = today.AddDays(-dayOffset);
            foreach (var (itemId, chance) in itemChances)
            {
                var adjustedChance = dayOffset > 7 ? chance * 0.85 : chance;
                if (rng.NextDouble() < adjustedChance)
                {
                    context.ActivityCompletions.Add(new ActivityCompletion
                    {
                        Id = Guid.NewGuid(),
                        ActivityItemId = itemId,
                        AccountId = accountId,
                        Date = date,
                        CompletedAtUtc = date.ToDateTime(
                            new TimeOnly(rng.Next(7, 22), rng.Next(0, 60)),
                            DateTimeKind.Utc),
                        CreatedAt = now
                    });
                }
            }
        }
    }
}
