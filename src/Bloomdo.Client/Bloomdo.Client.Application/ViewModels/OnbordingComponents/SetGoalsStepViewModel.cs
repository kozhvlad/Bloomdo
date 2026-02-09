using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Bloomdo.Application.ViewModels.Items;

namespace Bloomdo.Application.ViewModels.OnbordingComponents;

public partial class SetGoalsStepViewModel : PageViewModel
{
    public OnboardingViewModel? Parent { get; set; }

    public ObservableCollection<GoalItemViewModel> AvailableGoals { get; } = new()
    {
        new GoalItemViewModel("Career growth"),
        new GoalItemViewModel("Mental health"),
        new GoalItemViewModel("Goal setting"),
        new GoalItemViewModel("Focus & productivity"),
        new GoalItemViewModel("Habits & routines"),
        new GoalItemViewModel("Learning & skills")
    };

    public SetGoalsStepViewModel()
    {
        foreach (var goal in AvailableGoals)
        {
            goal.PropertyChanged += Goal_PropertyChanged;
            goal.CanSelectMoreProvider = () => CanSelectMore;
        }
    }

    public int SelectedCount => AvailableGoals.Count(g => g.IsSelected);

    public int MaxSelection => 3;
    public bool CanSelectMore => SelectedCount < MaxSelection;
    public bool CanContinue => SelectedCount >= 1;
    public string SelectionCounter => $"{SelectedCount}/{MaxSelection}";
    public IEnumerable<string> SelectedGoals => AvailableGoals.Where(g => g.IsSelected).Select(g => g.Name);

    private void Goal_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GoalItemViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(CanSelectMore));
            OnPropertyChanged(nameof(CanContinue));
            OnPropertyChanged(nameof(SelectionCounter));

            foreach (var goal in AvailableGoals)
            {
                goal.NotifyCanSelectMoreChanged();
            }
        }
    }

    [RelayCommand]
    private void Back()
    {
        Parent?.PreviousStepCommand.Execute(null);
    }

    [RelayCommand]
    private void Continue()
    {
        Parent?.NextStepCommand.Execute(null);
    }
}
