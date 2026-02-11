using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Application.ViewModels.OnbordingComponents;
using Bloomdo.Client.UI.MainComponents;
using Bloomdo.Client.UI.OnbordingComponents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.UI;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        switch (data)
        {
            case OnboardingViewModel:
                return new OnboardingView();

            case WelcomeStepViewModel:
                return new WelcomeView();

            case AskNameStepViewModel:
                return new AskNameView();

            case SetGoalsStepViewModel:
                return new SetGoalsView();

            case ShellViewModel:
                return new ShellView();
                
            case LoginViewModel:
                return new LoginView();

            case RegisterViewModel:
                return new RegisterView();

            case AccessDeniedViewModel:
                return new AccessDeniedView();

            case MainViewModel:
                return new MainView();
                
            case HomeViewModel:
                return new HomeView();
                
            case BlocksViewModel:
                return new BlocksView();
                
            case StatsViewModel:
                return new StatsView();
                
            case ProfileViewModel:
                return new ProfileView();

            default:
                var message = data is null
                    ? "Data passed to ViewLocator was null."
                    : $"No view found for view model of type {data.GetType().Name}.";
                return new TextBlock { Text = message };
        }
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}