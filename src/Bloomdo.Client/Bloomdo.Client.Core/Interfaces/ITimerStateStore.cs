using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Core.Interfaces;

public interface ITimerStateStore
{
    Task SaveAsync(TimerStateSnapshot state);
    Task<TimerStateSnapshot?> LoadAsync(Guid taskId);
    Task ClearAsync(Guid taskId);

    /// <summary>
    /// Returns all snapshots for today. Stale (other-date) files are auto-purged.
    /// </summary>
    Task<List<TimerStateSnapshot>> GetAllActiveAsync(CancellationToken ct = default);
}
