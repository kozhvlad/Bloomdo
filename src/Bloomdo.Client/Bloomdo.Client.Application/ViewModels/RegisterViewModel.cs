using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class RegisterViewModel : PageViewModel
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public RegisterViewModel(
        IAccessTokenManager tokenManager, 
        INavigationService navigationService,
        IToastService toastService,
        IPreferencesService preferencesService)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _toastService = toastService;
        _preferencesService = preferencesService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();

        // Pre-fill from onboarding data
        var onboardingName = _preferencesService.Get("Onboarding_Name", string.Empty);
        var onboardingTag = _preferencesService.Get("Onboarding_Tag", string.Empty);

        if (!string.IsNullOrWhiteSpace(onboardingName) && string.IsNullOrWhiteSpace(FirstName))
            FirstName = onboardingName;

        if (!string.IsNullOrWhiteSpace(onboardingTag) && string.IsNullOrWhiteSpace(Username))
            Username = onboardingTag;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required";
            _toastService.ShowWarning("Validation", "Email and password are required");
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || Username.Trim().Length < 3)
        {
            ErrorMessage = "A unique @tag (at least 3 characters) is required";
            _toastService.ShowWarning("Validation", "@tag is required");
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            _toastService.ShowWarning("Validation", "Passwords do not match");
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
            _toastService.ShowWarning("Validation", "Password must be at least 6 characters");
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var tag = Username.Trim().TrimStart('@').ToLowerInvariant();

            var success = await _tokenManager.RegisterAsync(
                Email,
                Password,
                tag,
                string.IsNullOrWhiteSpace(FirstName) ? null : FirstName,
                string.IsNullOrWhiteSpace(LastName) ? null : LastName
            );

            if (success)
            {
                _toastService.ShowSuccess("Welcome!", "Registration successful");
                _navigationService.NavigateTo<MainViewModel>();
            }
            else
            {
                ErrorMessage = "Registration failed. Email may already be in use.";
                _toastService.ShowError("Registration Failed", "Email may already be in use.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration failed: {ex.Message}";
            _toastService.ShowError("Error", $"Registration failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        _navigationService.NavigateTo<LoginViewModel>();
    }
}

