using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Core.Interfaces;

public interface ITimerStateStore
{
    Task SaveAsync(TimerStateSnapshot state);
    Task<TimerStateSnapshot?> LoadAsync(Guid taskId);
    Task ClearAsync(Guid taskId);
}
