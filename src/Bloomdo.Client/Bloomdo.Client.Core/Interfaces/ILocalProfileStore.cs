using Bloomdo.Client.Domain.Models;

namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Persists the user's own profile data locally so it can be
/// displayed when the device is offline.
/// </summary>
public interface ILocalProfileStore
{
    Task SaveAsync(LocalProfileSnapshot snapshot);
    Task<LocalProfileSnapshot?> LoadAsync();
}
