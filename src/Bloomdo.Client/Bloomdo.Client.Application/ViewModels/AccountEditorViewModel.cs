using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class AccountEditorViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;
    private readonly IProfileApiService _profileApiService;
    private readonly IToastService _toastService;

    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _bio = string.Empty;
    [ObservableProperty] private string _email = string.Empty;

    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmNewPassword = string.Empty;

    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _passwordErrorMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isChangingPassword;

    public AccountEditorViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IProfileApiService profileApiService,
        IToastService toastService)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _profileApiService = profileApiService;
        _toastService = toastService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadCurrentProfileAsync();
    }

    private async Task LoadCurrentProfileAsync()
    {
        var user = _tokenManager.CurrentUser;

        if (user == null)
        {
            try
            {
                user = await _profileApiService.GetProfileAsync();
                if (user != null)
                    _tokenManager.UpdateCurrentUser(user);
            }
            catch
            {
                // Profile fetch failed – fields will stay empty
            }
        }

        if (user == null) return;

        FirstName = user.FirstName ?? string.Empty;
        LastName = user.LastName ?? string.Empty;
        Username = user.Username ?? string.Empty;
        Bio = user.Bio ?? string.Empty;
        Email = user.Email;

        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmNewPassword = string.Empty;
        ErrorMessage = string.Empty;
        PasswordErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsSaving) return;

        ErrorMessage = string.Empty;

        var trimmedTag = Username.Trim().TrimStart('@').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(trimmedTag) || trimmedTag.Length < 3)
        {
            ErrorMessage = "@tag is required (at least 3 characters).";
            return;
        }

        IsSaving = true;

        try
        {
            var request = new UpdateProfileRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Username = trimmedTag,
                Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio.Trim(),
                Avatar = _tokenManager.CurrentUser?.Avatar
            };

            var result = await _profileApiService.UpdateProfileAsync(request);

            if (result != null)
            {
                _tokenManager.UpdateCurrentUser(result);
                _toastService.ShowSuccess("Profile updated!");
            }
            else
            {
                ErrorMessage = "Failed to update profile. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update profile failed: {ex.Message}");
            ErrorMessage = "An error occurred while saving.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ChangePassword()
    {
        PasswordErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            PasswordErrorMessage = "Enter your current password.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordErrorMessage = "Enter a new password.";
            return;
        }

        if (NewPassword.Length < 6)
        {
            PasswordErrorMessage = "New password must be at least 6 characters.";
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            PasswordErrorMessage = "Passwords do not match.";
            return;
        }

        _toastService.ShowInfo("Password change coming soon!");
    }

    [RelayCommand]
    private void EditAvatar()
    {
        _navigationService.NavigateTo<ProfileEditorViewModel>();
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateBack();
    }
}
