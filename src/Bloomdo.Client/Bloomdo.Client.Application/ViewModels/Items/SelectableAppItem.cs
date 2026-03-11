using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class SelectableAppItem : ObservableObject
{
    public string PackageName { get; }
    public string AppLabel { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SelectableAppItem(string packageName, string appLabel, bool isSelected = false)
    {
        PackageName = packageName;
        AppLabel = appLabel;
        _isSelected = isSelected;
    }
}
