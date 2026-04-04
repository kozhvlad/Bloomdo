using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountRole> AccountRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<AppUsageRecord> AppUsageRecords { get; set; }
    public DbSet<DailySnapshot> DailySnapshots { get; set; }
    public DbSet<BlockRule> BlockRules { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<AccountAchievement> AccountAchievements { get; set; }
    public DbSet<ActivityGroup> ActivityGroups { get; set; }
    public DbSet<ActivityItem> ActivityItems { get; set; }
    public DbSet<ActivityCompletion> ActivityCompletions { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<StreakFreeze> StreakFreezes { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<GroupMembership> GroupMemberships { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Role configuration (lookup table, not BaseEntity)
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Description).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();

            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(20);
            entity.Property(e => e.Bio).HasMaxLength(500);

            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasFilter("\"Username\" IS NOT NULL AND \"IsDeleted\" = false");

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasMany(e => e.AccountRoles)
                .WithOne(ar => ar.Account)
                .HasForeignKey(ar => ar.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RefreshTokens)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(45);
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.RoleId).IsRequired();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Permission)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(e => new { e.RoleId, e.Permission }).IsUnique();

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AccountRole configuration (many-to-many join entity)
        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.HasOne(e => e.Role)
                .WithMany(r => r.AccountRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.AccountId, e.RoleId }).IsUnique();

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AppUsageRecord configuration
        modelBuilder.Entity<AppUsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.PackageName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.AppLabel).HasMaxLength(256);
            entity.Property(e => e.ForegroundSeconds).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.AccountId, e.Date, e.PackageName }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DailySnapshot configuration
        modelBuilder.Entity<DailySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.AccountId, e.Date }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // BlockRule configuration
        modelBuilder.Entity<BlockRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.BlockedPackagesJson).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.ScheduleDaysJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Achievement configuration
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.Icon).HasMaxLength(16);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.Code).IsUnique();

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AccountAchievement configuration
        modelBuilder.Entity<AccountAchievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.AccountId, e.AchievementId }).IsUnique();

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Achievement)
                .WithMany(a => a.AccountAchievements)
                .HasForeignKey(e => e.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ActivityGroup configuration
        modelBuilder.Entity<ActivityGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Title).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Icon).HasMaxLength(16);
            entity.Property(e => e.Color).HasMaxLength(16);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.Group)
                .HasForeignKey(i => i.ActivityGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ActivityItem configuration
        modelBuilder.Entity<ActivityItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.Icon).HasMaxLength(16).HasDefaultValue("✨");
            entity.Property(e => e.Color).HasMaxLength(16).HasDefaultValue("#7E57C2");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasMany(e => e.Completions)
                .WithOne(c => c.ActivityItem)
                .HasForeignKey(c => c.ActivityItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ActivityCompletion configuration
        modelBuilder.Entity<ActivityCompletion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Note).HasMaxLength(512);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.ActivityItemId, e.AccountId, e.Date }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Friendship configuration
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.HasIndex(e => new { e.RequesterId, e.AddresseeId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Requester)
                .WithMany(a => a.InitiatedFriendships)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Addressee)
                .WithMany(a => a.ReceivedFriendships)
                .HasForeignKey(e => e.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // GroupMembership configuration
        modelBuilder.Entity<GroupMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.HasIndex(e => new { e.ActivityGroupId, e.AccountId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(e => e.ActivityGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.GroupMemberships)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ChatConversation configuration
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Role).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.StripeCustomerId).HasMaxLength(256);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(256);
            entity.Property(e => e.Plan).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrentPeriodStart).IsRequired();
            entity.Property(e => e.CurrentPeriodEnd).IsRequired();
            entity.Property(e => e.CancelAtPeriodEnd).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.AccountId).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => e.StripeCustomerId);
            entity.HasIndex(e => e.StripeSubscriptionId);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // StreakFreeze configuration
        modelBuilder.Entity<StreakFreeze>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.AccountId, e.Date }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(e => e.Recipient)
                .WithMany()
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.RecipientId, e.IsRead });

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        SeedData(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds roles and their default permission mappings.
    /// Roles inherit permissions from lower privilege levels.
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed Roles — Id values match the UserRole enum
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = (int)UserRole.User, Name = nameof(UserRole.User), Description = "Standard user", CreatedAt = seedDate },
            new Role { Id = (int)UserRole.Premium, Name = nameof(UserRole.Premium), Description = "Premium subscriber", CreatedAt = seedDate },
            new Role { Id = (int)UserRole.Moderator, Name = nameof(UserRole.Moderator), Description = "Content moderator", CreatedAt = seedDate },
            new Role { Id = (int)UserRole.Admin, Name = nameof(UserRole.Admin), Description = "System administrator", CreatedAt = seedDate }
        );

        // Seed RolePermissions — hierarchical inheritance
        var seedId = 1;
        var entries = new List<object>();

        string[] userPermissions =
        [
            Permissions.ProfileView, Permissions.ProfileEdit,
            Permissions.GoalsCreate, Permissions.GoalsEdit, Permissions.GoalsDelete,
            Permissions.BlocksManage,
            Permissions.ActivitiesManage,
            Permissions.StatsView,
            Permissions.ChatAccess
        ];

        string[] premiumPermissions =
        [
            ..userPermissions,
            Permissions.PremiumAccess,
            Permissions.StatsExport
        ];

        string[] moderatorPermissions =
        [
            ..premiumPermissions,
            Permissions.UsersView,
            Permissions.UsersManage
        ];

        string[] adminPermissions =
        [
            ..moderatorPermissions,
            Permissions.RolesManage,
            Permissions.SettingsManage,
            Permissions.AnalyticsView
        ];

        var roleMap = new Dictionary<UserRole, string[]>
        {
            [UserRole.User] = userPermissions,
            [UserRole.Premium] = premiumPermissions,
            [UserRole.Moderator] = moderatorPermissions,
            [UserRole.Admin] = adminPermissions
        };

        foreach (var (role, perms) in roleMap)
        {
            foreach (var perm in perms)
            {
                entries.Add(new
                {
                    Id = new Guid($"00000000-0000-0000-0000-{seedId++:D12}"),
                    RoleId = (int)role,
                    Permission = perm,
                    CreatedAt = seedDate,
                    IsDeleted = false
                });
            }
        }

        modelBuilder.Entity<RolePermission>().HasData(entries.ToArray());

        // Seed Achievements
        modelBuilder.Entity<Achievement>().HasData(
            new Achievement { Id = new Guid("a0000000-0000-0000-0000-000000000001"), Code = "streak_3", Title = "Getting Started", Description = "Achieve a 3-day streak", Icon = "🔥", SortOrder = 1, CreatedAt = seedDate },
            new Achievement { Id = new Guid("a0000000-0000-0000-0000-000000000002"), Code = "streak_7", Title = "Week Warrior", Description = "Achieve a 7-day streak", Icon = "⚡", SortOrder = 2, CreatedAt = seedDate },
            new Achievement { Id = new Guid("a0000000-0000-0000-0000-000000000003"), Code = "streak_14", Title = "Two Weeks Strong", Description = "Achieve a 14-day streak", Icon = "💪", SortOrder = 3, CreatedAt = seedDate },
            new Achievement { Id = new Guid("a0000000-0000-0000-0000-000000000004"), Code = "streak_30", Title = "Monthly Master", Description = "Achieve a 30-day streak", Icon = "🏆", SortOrder = 4, CreatedAt = seedDate },
            new Achievement { Id = new Guid("a0000000-0000-0000-0000-000000000005"), Code = "streak_100", Title = "Century Club", Description = "Achieve a 100-day streak", Icon = "👑", SortOrder = 5, CreatedAt = seedDate }
        );
    }
}
