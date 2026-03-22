namespace Bloomdo.Client.Core;

/// <summary>
/// Application-level configuration flags.
/// Edit these values to control app behavior during development, demos and testing.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// When true, the onboarding flow is always shown on app start,
    /// regardless of whether it was previously completed.
    /// Set to false for normal production behavior.
    /// </summary>
    public static bool ForceShowOnboarding { get; set; } = false;

    /// <summary>
    /// When true, bypasses authentication check and navigates directly to the main screen.
    /// Useful for UI development and layout work.
    /// </summary>
    public static bool SkipAuthentication { get; set; } = false;
}
