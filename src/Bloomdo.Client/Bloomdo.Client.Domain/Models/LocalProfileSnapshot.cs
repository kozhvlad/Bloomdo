using Bloomdo.Shared.DTOs.Profile;

namespace Bloomdo.Client.Domain.Models;

/// <summary>
/// Locally cached profile data so the user can view their profile, stats,
/// followers/following counts, and subscription status while offline.
/// </summary>
public sealed class LocalProfileSnapshot
{
    public DateTime LastUpdatedUtc { get; set; }

    // ── Profile basics ──────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string JoinedDateText { get; set; } = string.Empty;

    // ── Avatar (serialized as JSON-friendly object) ─────────────
    public AvatarConfig? Avatar { get; set; }

    // ── Social ──────────────────────────────────────────────────
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    // ── Stats ───────────────────────────────────────────────────
    public int StreakDays { get; set; }
    public int TasksCompleted { get; set; }
    public int FocusHours { get; set; }
    public int TotalBlocksCreated { get; set; }
    public int AchievementsUnlocked { get; set; }
    public string Level { get; set; } = "Member";

    // ── Subscription ────────────────────────────────────────────
    public bool IsPremium { get; set; }
}
