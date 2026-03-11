using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Core.Interfaces;

public interface IInstalledAppsService
{
    Task<IReadOnlyList<InstalledAppInfo>> GetInstalledAppsAsync();
}
