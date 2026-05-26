using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.Enums;
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
    private readonly IProfileApiService _profileApiService;

    private bool _isLoadingPrivacy;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private int _profileVisibilityIndex;

    public SettingsViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IThemeService themeService,
        IToastService toastService,
        IPreferencesService preferencesService,
        IProfileApiService profileApiService)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _themeService = themeService;
        _toastService = toastService;
        _preferencesService = preferencesService;
        _profileApiService = profileApiService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        IsDarkTheme = _themeService.IsDarkTheme;
        _ = LoadPrivacyAsync();
    }

    private async Task LoadPrivacyAsync()
    {
        _isLoadingPrivacy = true;
        try
        {
            var profile = await _profileApiService.GetProfileAsync();
            if (profile != null)
                ProfileVisibilityIndex = (int)profile.ProfileVisibility;
        }
        catch { /* keep default */ }
        finally
        {
            _isLoadingPrivacy = false;
        }
    }

    partial void OnProfileVisibilityIndexChanged(int value)
    {
        if (_isLoadingPrivacy) return;
        _ = SavePrivacyAsync((ProfileVisibility)value);
    }

    private async Task SavePrivacyAsync(ProfileVisibility visibility)
    {
        try
        {
            await _profileApiService.UpdateProfileAsync(new UpdateProfileRequest
            {
                ProfileVisibility = visibility
            });
            _toastService.ShowSuccess("Privacy updated!");
        }
        catch
        {
            _toastService.ShowError("Could not update privacy.");
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _themeService.SetDarkTheme(value);
    }

    [RelayCommand]
    private void Done()
    {
        _navigationService.NavigateBack();
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
