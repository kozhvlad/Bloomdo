using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class AvatarPartOption : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _colorHex = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
