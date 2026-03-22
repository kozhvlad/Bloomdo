using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class MostUsedAppViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    private string _name;

    [ObservableProperty]
    private string _duration;

    [ObservableProperty]
    private double _usagePercent;

    [ObservableProperty]
    private int _totalSeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIcon))]
    private byte[]? _iconBytes;

    public string Icon => Name.Length > 0 ? Name[..1] : "?";
    public bool HasIcon => IconBytes is { Length: > 0 };

    public MostUsedAppViewModel(string name, string duration, byte[]? iconBytes = null)
    {
        _name = name;
        _duration = duration;
        _iconBytes = iconBytes;
    }

    public MostUsedAppViewModel(string name, string duration, int totalSeconds, double usagePercent, byte[]? iconBytes = null)
    {
        _name = name;
        _duration = duration;
        _totalSeconds = totalSeconds;
        _usagePercent = usagePercent;
        _iconBytes = iconBytes;
    }
}