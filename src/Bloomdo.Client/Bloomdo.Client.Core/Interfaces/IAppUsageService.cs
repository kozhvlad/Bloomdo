using Bloomdo.Core.Models;

namespace Bloomdo.Core.Interfaces
{
    public interface IAppUsageService
    {
        Task<IReadOnlyList<AppUsageInfo>> GetTodayUsageAsync();
        Task<int> GetPickupsTodayAsync();
    }
}
