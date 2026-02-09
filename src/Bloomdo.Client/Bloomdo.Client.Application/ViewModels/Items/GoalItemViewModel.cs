using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Application.ViewModels.Items;

public class GoalItemViewModel : ObservableObject
{
    public string Name { get; }

    public Func<bool>? CanSelectMoreProvider { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value && CanSelectMoreProvider is Func<bool> canSelect && !canSelect())
            {
                return;
            }
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public bool IsEnabled => IsSelected || (CanSelectMoreProvider?.Invoke() ?? true);

    public void NotifyCanSelectMoreChanged()
    {
        OnPropertyChanged(nameof(IsEnabled));
    }

    public GoalItemViewModel(string name)
    {
        Name = name;
    }
}