using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bloomdo.Application.ViewModels;
using Bloomdo.Application.ViewModels.OnbordingComponents;
using Bloomdo.Application.ViewModels.MainComponents;
using Bloomdo.UI.OnbordingComponents;
using Bloomdo.UI.MainComponents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.UI;

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