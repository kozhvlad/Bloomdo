using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Application.ViewModels.Items;

public partial class MostUsedAppViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    private string _name;

    [ObservableProperty]
    private string _duration;

    public string Icon => Name.Length > 0 ? Name[..1] : "?";

    public MostUsedAppViewModel(string name, string duration)
    {
        _name = name;
        _duration = duration;
    }
}