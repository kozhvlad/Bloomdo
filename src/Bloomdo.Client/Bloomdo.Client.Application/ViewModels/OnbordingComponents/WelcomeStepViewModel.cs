using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Application.ViewModels.OnbordingComponents;

public partial class WelcomeStepViewModel : PageViewModel
{
    public OnboardingViewModel? Parent { get; set; }

    [RelayCommand]
    private void Continue()
    {
        Parent?.NextStepCommand.Execute(null);
    }
}
