using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Application.ViewModels.OnbordingComponents;

public partial class AskNameStepViewModel : PageViewModel
{
    public OnboardingViewModel? Parent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(IsInvalid))]
    private string _name = "Vladyslav";

    partial void OnNameChanged(string value)
    {
        ContinueCommand.NotifyCanExecuteChanged();
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(TrimmedName);

    public bool IsInvalid => !IsValid;

    public string TrimmedName => (_name).Trim();

    [RelayCommand(CanExecute = nameof(IsValid))]
    private void Continue()
    {
        if (!IsValid)
        {
            return;
        }
        
        Parent?.NextStepCommand.Execute(null);
    }

    [RelayCommand]
    private void Back()
    {
        Parent?.PreviousStepCommand.Execute(null);
    }
}
