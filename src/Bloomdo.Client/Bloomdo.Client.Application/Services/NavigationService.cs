using System.Diagnostics;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Enums;
using Bloomdo.Client.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Bloomdo.Client.Application.Services;

public class NavigationService(
    IServiceProvider serviceProvider,
    IAuthorizationService authorizationService,
    IToastService toastService,
    ShellViewModel shellViewModel)
    : INavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : IPage
    {
        var viewModelType = typeof(TViewModel);

        var authResult = authorizationService.CheckAccess(viewModelType);

        if (!authResult.IsAuthorized)
        {
            HandleUnauthorizedAccess(authResult);
            return;
        }

        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        shellViewModel.SetViewModel(viewModel);
    }

    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : IPage
    {
        var viewModelType = typeof(TViewModel);

        var authResult = authorizationService.CheckAccess(viewModelType);

        if (!authResult.IsAuthorized)
        {
            HandleUnauthorizedAccess(authResult);
            return;
        }

        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        configure(viewModel);
        shellViewModel.SetViewModel(viewModel);
    }

    public void NavigateBack() => shellViewModel.NavigateBack();

    public void OnboardingComplete()
    {
        Debug.WriteLine("OnboardingComplete called - saving flag and navigating to LoginViewModel");
        try
        {
            shellViewModel.CompleteOnboarding();
            NavigateTo<LoginViewModel>();
            Debug.WriteLine("LoginViewModel set in ShellViewModel after onboarding");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnboardingComplete: {ex.Message}");
            throw;
        }
    }

    private void HandleUnauthorizedAccess(AuthorizationResult authResult)
    {
        switch (authResult.FailureType)
        {
            case AuthorizationFailureType.NotAuthenticated:
                var loginViewModel = serviceProvider.GetRequiredService<LoginViewModel>();
                shellViewModel.SetViewModel(loginViewModel);
                toastService.ShowWarning("Authentication required");
                break;

            case AuthorizationFailureType.InsufficientRole:
            case AuthorizationFailureType.InsufficientPermission:
            case AuthorizationFailureType.PolicyNotMet:
                var accessDeniedViewModel = serviceProvider.GetRequiredService<AccessDeniedViewModel>();
                accessDeniedViewModel.SetMessage(authResult.FailureReason ?? "Access denied");
                shellViewModel.SetViewModel(accessDeniedViewModel);
                toastService.ShowError(authResult.FailureReason ?? "Access denied");
                break;
        }

        Debug.WriteLine($"Unauthorized access attempt: {authResult.FailureReason}");
    }
}