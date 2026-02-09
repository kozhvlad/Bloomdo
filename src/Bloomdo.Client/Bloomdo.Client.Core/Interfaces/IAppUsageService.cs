using Bloomdo.Client.Core.Models;

namespace Bloomdo.Client.Core.Interfaces;

public interface IAppUsageService
{
    Task<IReadOnlyList<AppUsageInfo>> GetTodayUsageAsync();
    Task<int> GetPickupsTodayAsync();
}