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
            Permissions.StatsView
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
    }
}
