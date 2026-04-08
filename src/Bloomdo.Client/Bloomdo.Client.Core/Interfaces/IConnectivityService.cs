namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Monitors network connectivity state.
/// </summary>
public interface IConnectivityService
{
    bool IsOnline { get; }
    event Action<bool>? ConnectivityChanged;
}
