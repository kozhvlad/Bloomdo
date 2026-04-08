using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class NoConnectionViewModel : PageViewModel
{
    private readonly ShellViewModel _shellViewModel;
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;

    [ObservableProperty]
    private string _message = "Unable to connect to the server. Please check your internet connection and try again.";

    [ObservableProperty]
    private bool _isRetrying;

    /// <summary>
    /// True when the user was previously authenticated and can continue in offline mode.
    /// </summary>
    public bool CanContinueOffline { get; }

    public NoConnectionViewModel(
        ShellViewModel shellViewModel,
        INavigationService navigationService,
        IAccessTokenManager tokenManager)
    {
        _shellViewModel = shellViewModel;
        _navigationService = navigationService;
        _tokenManager = tokenManager;

        CanContinueOffline = tokenManager.IsAuthenticated;
    }

    public void SetMessage(string message)
    {
        Message = message;
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        IsRetrying = true;

        try
        {
            await _shellViewModel.InitializeAsync();
        }
        finally
        {
            IsRetrying = false;
        }
    }

    [RelayCommand]
    private void ContinueOffline()
    {
        _navigationService.NavigateTo<MainViewModel>();
    }
}
