using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.OnbordingComponents;

public partial class WelcomeStepViewModel : PageViewModel
{
    public OnboardingViewModel? Parent { get; set; }

    [RelayCommand]
    private void Continue()
    {
        Parent?.NextStepCommand.Execute(null);
    }
}
