using Bloomdo.Application.ViewModels;
using Bloomdo.Application.ViewModels.MainComponents;
using Bloomdo.Core.Attributes;
using Bloomdo.Core.Interfaces;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Bloomdo.Application.Services;

public class NavigationService(
    IServiceProvider serviceProvider,
    IAccessTokenManager authService,
    ShellViewModel shellViewModel)
    : INavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : IPage
    {
        var viewModelType = typeof(TViewModel);

        if (viewModelType.GetCustomAttribute<AuthorizeAttribute>() != null)
        {
            if (!authService.IsAuthenticated)
            {
                var onboardingViewModel = serviceProvider.GetRequiredService<LoginViewModel>();
                shellViewModel.SetViewModel(onboardingViewModel); 
                return;
            }
        }

        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        shellViewModel.SetViewModel(viewModel);
    }

    public void OnboardingComplete(string name, string goals)
    {
        Debug.WriteLine("OnboardingComplete called - navigating to MainViewModel");
        try
        {
            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
            Debug.WriteLine($"MainViewModel created: {mainViewModel != null}");
            shellViewModel.SetViewModel(mainViewModel);
            Debug.WriteLine("MainViewModel set in ShellViewModel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnboardingComplete: {ex.Message}");
            throw;
        }
    }
}