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

    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;

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
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            AvatarEditor.Initialize(user.Avatar, _ => { });
            return;
        }

        var profile = await _profileApiService.GetProfileAsync();
        if (profile != null)
        {
            _tokenManager.UpdateCurrentUser(profile);
            AvatarEditor.Initialize(profile.Avatar, _ => { });
        }
    }

    public void Initialize()
    {
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            AvatarEditor.Initialize(user.Avatar, _ => { });
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;

        ErrorMessage = string.Empty;
        IsSaving = true;

        try
        {
            var request = new UpdateProfileRequest
            {
                Avatar = AvatarEditor.BuildAvatarConfig()
            };

            var result = await _profileApiService.UpdateProfileAsync(request);

            if (result != null)
            {
                _tokenManager.UpdateCurrentUser(result);
                _navigationService.NavigateBack();
            }
            else
            {
                ErrorMessage = "Failed to save avatar. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save avatar failed: {ex.Message}");
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
        _navigationService.NavigateBack();
    }
}
