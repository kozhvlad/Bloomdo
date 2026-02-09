namespace Bloomdo.Client.Core.Models;

public sealed class AppUsageInfo
{
	public string PackageName { get; init; } = string.Empty;
	public string? AppLabel { get; init; }
	public TimeSpan ForegroundTime { get; init; }
}
