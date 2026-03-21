namespace Bloomdo.Client.Core.Interfaces;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void SetDarkTheme(bool isDark);
}
