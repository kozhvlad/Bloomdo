using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Infrastructure.Services;

public class ConnectivityService : IConnectivityService, IDisposable
{
    private bool _isOnline;
    private bool _initialized;
    private readonly object _lock = new();

    public bool IsOnline
    {
        get
        {
            EnsureInitialized();
            return _isOnline;
        }
    }

    public event Action<bool>? ConnectivityChanged;

    private void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            try
            {
                _isOnline = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess
                            == Microsoft.Maui.Networking.NetworkAccess.Internet;
                Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged += OnPlatformConnectivityChanged;
            }
            catch
            {
                _isOnline = false;
            }
            _initialized = true;
        }
    }

    private void OnPlatformConnectivityChanged(object? sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
    {
        var newState = e.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;
        if (newState == _isOnline) return;

        _isOnline = newState;
        ConnectivityChanged?.Invoke(newState);
    }

    public void Dispose()
    {
        if (!_initialized) return;
        try
        {
            Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged -= OnPlatformConnectivityChanged;
        }
        catch
        {
            // Platform may not be available during teardown
        }
    }
}
