using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class SelectableAppItem : ObservableObject
{
    public string PackageName { get; }
    public string AppLabel { get; }
    public byte[]? IconBytes { get; }
    public bool HasIcon => IconBytes is { Length: > 0 };

    [ObservableProperty]
    private bool _isSelected;

    public SelectableAppItem(string packageName, string appLabel, bool isSelected = false, byte[]? iconBytes = null)
    {
        PackageName = packageName;
        AppLabel = appLabel;
        _isSelected = isSelected;
        IconBytes = iconBytes;
    }
}
