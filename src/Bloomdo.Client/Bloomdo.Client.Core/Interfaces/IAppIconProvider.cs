namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Provides app icon images (as PNG byte arrays) by package name.
/// Icons are lazily loaded and cached in memory.
/// </summary>
public interface IAppIconProvider
{
    /// <summary>
    /// Returns the icon for the given package as a PNG byte array, or null if unavailable.
    /// </summary>
    byte[]? GetIcon(string packageName);
}
