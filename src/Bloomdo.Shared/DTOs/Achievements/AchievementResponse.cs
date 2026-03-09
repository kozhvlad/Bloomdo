namespace Bloomdo.Shared.DTOs.Achievements;

public sealed class AchievementResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public bool IsUnlocked { get; init; }
    public DateOnly? UnlockedAt { get; init; }
}
