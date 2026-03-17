using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class ProfileEditorViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;
    private readonly IProfileApiService _profileApiService;
    private Action? _onSaved;

    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _bio = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _initials = "?";
    [ObservableProperty] private AvatarConfig? _currentAvatar;

    public AvatarEditorViewModel AvatarEditor { get; }

    public ProfileEditorViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IProfileApiService profileApiService,
        AvatarEditorViewModel avatarEditor)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _profileApiService = profileApiService;
        AvatarEditor = avatarEditor;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        Initialize();
    }

    public void Initialize(Action? onSaved = null)
    {
        _onSaved = onSaved;
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            FirstName = user.FirstName ?? string.Empty;
            LastName = user.LastName ?? string.Empty;
            Username = user.Username ?? string.Empty;
            Bio = user.Bio ?? string.Empty;
            CurrentAvatar = user.Avatar;
            UpdateInitials();

            AvatarEditor.Initialize(user.Avatar, avatar =>
            {
                CurrentAvatar = avatar;
            });
        }
    }

    partial void OnFirstNameChanged(string value) => UpdateInitials();
    partial void OnLastNameChanged(string value) => UpdateInitials();

    private void UpdateInitials()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            Initials = $"{FirstName[0]}{LastName[0]}".ToUpper();
        else if (!string.IsNullOrEmpty(FirstName))
            Initials = $"{FirstName[0]}".ToUpper();
        else
            Initials = "?";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "First name is required.";
            return;
        }

        IsSaving = true;

        try
        {
            var request = new UpdateProfileRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Username = Username.Trim(),
                Bio = Bio.Trim(),
                Avatar = AvatarEditor.BuildAvatarConfig()
            };

            var result = await _profileApiService.UpdateProfileAsync(request);

            if (result != null)
            {
                _onSaved?.Invoke();
                _navigationService.NavigateTo<MainViewModel>();
            }
            else
            {
                ErrorMessage = "Failed to update profile. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save profile failed: {ex.Message}");
            ErrorMessage = "An error occurred while saving.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateTo<MainViewModel>();
    }
}
