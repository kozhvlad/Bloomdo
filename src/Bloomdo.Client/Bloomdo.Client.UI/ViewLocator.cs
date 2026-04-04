using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Application.ViewModels.OnbordingComponents;
using Bloomdo.Client.UI.Dialogs;
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

            case NoConnectionViewModel:
                return new NoConnectionView();

            case MainViewModel:
                return new MainView();
                
            case HomeViewModel:
                return new HomeView();

            case SocialViewModel:
                return new SocialView();

            case UserSearchViewModel:
                return new UserSearchView();

            case NotificationsViewModel:
                return new NotificationsView();

            case FollowListViewModel:
                return new FollowListView();

            case SharedGroupDetailViewModel:
                return new SharedGroupDetailView();

            case SharedGroupEditorViewModel:
                return new SharedGroupEditorView();

            case BlocksViewModel:
                return new BlocksView();

            case BlockEditorViewModel:
                return new BlockEditorView();

            case StatsViewModel:
                return new StatsView();

            case AiChatViewModel:
                return new AiChatView();

            case SubscriptionViewModel:
                return new SubscriptionView();

            case ProfileViewModel:
                return new ProfileView();

            case ProfileEditorViewModel:
                return new ProfileEditorView();

            case AccountEditorViewModel:
                return new AccountEditorView();

            case SettingsViewModel:
                return new SettingsView();

            case GroupEditorViewModel:
                return new GroupEditorView();

            case TaskEditorViewModel:
                return new TaskEditorView();

            case TimerDialogViewModel:
                return new TimerDialogView();

            case ConfirmDialogViewModel:
                return new ConfirmDialogView();

            case PhotoVerificationViewModel:
                return new PhotoVerificationView();

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