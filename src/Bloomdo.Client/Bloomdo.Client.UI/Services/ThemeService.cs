using Avalonia.Styling;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class ThemeService : IThemeService
{
    private readonly IPreferencesService _preferences;
    private const string ThemeKey = "IsDarkTheme";

    public ThemeService(IPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public bool IsDarkTheme => _preferences.Get(ThemeKey, true);

    public void SetDarkTheme(bool isDark)
    {
        _preferences.Set(ThemeKey, isDark);

        if (Avalonia.Application.Current is not null)
        {
            Avalonia.Application.Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
