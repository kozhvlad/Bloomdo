using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;
    private readonly IThemeService _themeService;
    private readonly IToastService _toastService;
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    public SettingsViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IThemeService themeService,
        IToastService toastService,
        IPreferencesService preferencesService)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _themeService = themeService;
        _toastService = toastService;
        _preferencesService = preferencesService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        IsDarkTheme = _themeService.IsDarkTheme;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _themeService.SetDarkTheme(value);
    }

    [RelayCommand]
    private void Done()
    {
        _navigationService.NavigateTo<MainViewModel>(vm => vm.SelectedTabIndex = 3);
    }

    [RelayCommand]
    private void GoToProfile()
    {
        _navigationService.NavigateTo<AccountEditorViewModel>();
    }

    [RelayCommand]
    private void OpenHelpCenter()
    {
        _toastService.ShowInfo("Help Center coming soon!");
    }

    [RelayCommand]
    private void OpenFeedback()
    {
        _toastService.ShowInfo("Feedback coming soon!");
    }

    [RelayCommand]
    private void ResetOnboarding()
    {
        _preferencesService.Set("OnboardingCompleted", false);
        _toastService.ShowSuccess("Onboarding reset! Restart the app to see it.");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenManager.LogoutAsync();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
