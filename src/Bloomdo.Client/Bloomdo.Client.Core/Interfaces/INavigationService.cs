namespace Bloomdo.Core.Interfaces;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : IPage;

    void OnboardingComplete(string name, string goals);
}