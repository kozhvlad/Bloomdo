using Bloomdo.Client.Core.Interfaces;
using Microsoft.Maui.Storage;

namespace Bloomdo.Client.Infrastructure.Services;

public class PreferencesService : IPreferencesService
{
    public bool Get(string key, bool defaultValue)
    {
        return Preferences.Default.Get(key, defaultValue);
    }

    public void Set(string key, bool value)
    {
        Preferences.Default.Set(key, value);
    }
}
