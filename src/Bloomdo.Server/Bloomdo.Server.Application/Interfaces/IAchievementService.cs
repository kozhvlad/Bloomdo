using Bloomdo.Shared.DTOs.Achievements;

namespace Bloomdo.Server.Application.Interfaces;

public interface IAchievementService
{
    Task<List<AchievementResponse>> GetAchievementsAsync(Guid accountId, CancellationToken ct = default);
    Task EvaluateAchievementsAsync(Guid accountId, CancellationToken ct = default);
}
