namespace Bloomdo.Client.Domain.Models;

public sealed class InstalledAppInfo
{
    public string PackageName { get; init; } = string.Empty;
    public string AppLabel { get; init; } = string.Empty;
}
