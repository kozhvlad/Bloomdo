namespace Bloomdo.Client.Core.Interfaces;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : IPage;

    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : IPage;

    void NavigateBack();

    void OnboardingComplete();
}