using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class ProfileViewModel : PageViewModel
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly IProfileApiService _profileApiService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _bio = string.Empty;

    [ObservableProperty]
    private string _initials = "?";

    [ObservableProperty]
    private int _streakDays;

    [ObservableProperty]
    private int _tasksCompleted;

    [ObservableProperty]
    private int _focusHours;

    [ObservableProperty]
    private string _level = "Member";

    [ObservableProperty]
    private string _joinedDateText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private AvatarConfig? _currentAvatar;

    [ObservableProperty]
    private string _avatarBackgroundHex = "#7E57C2";

    [ObservableProperty]
    private string _avatarSkinHex = "#FDDBB4";

    [ObservableProperty]
    private string _avatarHairHex = "#2C2C2C";

    [ObservableProperty]
    private string _avatarClothingHex = "#66BB6A";

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private int _totalBlocksCreated;

    [ObservableProperty]
    private int _achievementsUnlocked;

    public ProfileViewModel(
        IAccessTokenManager tokenManager,
        INavigationService navigationService,
        IProfileApiService profileApiService)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _profileApiService = profileApiService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        IsLoading = true;
        try
        {
            // Load profile from cached user first
            ApplyUserData();

            // Refresh from server
            var profile = await _profileApiService.GetProfileAsync();
            if (profile != null)
            {
                Name = $"{profile.FirstName} {profile.LastName}".Trim();
                Email = profile.Email;
                Username = !string.IsNullOrEmpty(profile.Username) ? $"@{profile.Username}" : string.Empty;
                Bio = profile.Bio ?? string.Empty;
                JoinedDateText = $"Joined {profile.CreatedAt:MMMM yyyy}";

                if (!string.IsNullOrEmpty(profile.FirstName) && !string.IsNullOrEmpty(profile.LastName))
                    Initials = $"{profile.FirstName[0]}{profile.LastName[0]}".ToUpper();
                else if (!string.IsNullOrEmpty(profile.FirstName))
                    Initials = $"{profile.FirstName[0]}".ToUpper();

                ApplyAvatar(profile.Avatar);
            }

            // Load stats
            var stats = await _profileApiService.GetProfileStatsAsync();
            if (stats != null)
            {
                StreakDays = stats.StreakDays;
                TasksCompleted = stats.TasksCompleted;
                FocusHours = stats.FocusHours;
                TotalBlocksCreated = stats.TotalBlocksCreated;
                AchievementsUnlocked = stats.AchievementsUnlocked;

                Level = stats.StreakDays switch
                {
                    >= 100 => "🏆 Legend",
                    >= 50 => "⭐ Expert",
                    >= 30 => "🔥 Dedicated",
                    >= 7 => "💪 Committed",
                    _ => "🌱 Beginner"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load profile failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyUserData()
    {
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            Name = $"{user.FirstName} {user.LastName}".Trim();
            Email = user.Email;
            Username = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : string.Empty;
            Bio = user.Bio ?? string.Empty;
            JoinedDateText = $"Joined {user.CreatedAt:MMMM yyyy}";

            if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                Initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
            else if (!string.IsNullOrEmpty(user.FirstName))
                Initials = $"{user.FirstName[0]}".ToUpper();

            ApplyAvatar(user.Avatar);
        }
    }

    private void ApplyAvatar(AvatarConfig? avatar)
    {
        CurrentAvatar = avatar;

        if (avatar != null)
        {
            AvatarBackgroundHex = GetBackgroundColor(avatar.BackgroundColor);
            AvatarSkinHex = GetSkinColor(avatar.SkinTone);
            AvatarHairHex = GetHairColor(avatar.HairColor);
            AvatarClothingHex = GetClothingColor(avatar.ClothingColor);
        }
        else
        {
            AvatarBackgroundHex = "#7E57C2";
            AvatarSkinHex = "#FDDBB4";
            AvatarHairHex = "#2C2C2C";
            AvatarClothingHex = "#66BB6A";
        }
    }

    private static string GetBackgroundColor(int id) => id switch
    {
        0 => "#7E57C2",
        1 => "#42A5F5",
        2 => "#66BB6A",
        3 => "#FFA726",
        4 => "#EC407A",
        5 => "#26A69A",
        _ => "#7E57C2"
    };

    private static string GetSkinColor(int id) => id switch
    {
        0 => "#FDDBB4",
        1 => "#E8B98D",
        2 => "#D4915A",
        3 => "#B07040",
        4 => "#8B5E3C",
        5 => "#5C3A1E",
        _ => "#FDDBB4"
    };

    private static string GetHairColor(int id) => id switch
    {
        0 => "#2C2C2C",
        1 => "#6B4226",
        2 => "#F5D76E",
        3 => "#C0392B",
        4 => "#2980B9",
        5 => "#8E44AD",
        _ => "#2C2C2C"
    };

    private static string GetClothingColor(int id) => id switch
    {
        0 => "#66BB6A",
        1 => "#42A5F5",
        2 => "#EF5350",
        3 => "#AB47BC",
        4 => "#FFA726",
        5 => "#37474F",
        _ => "#66BB6A"
    };

    [RelayCommand]
    private void EditProfile()
    {
        _navigationService.NavigateTo<ProfileEditorViewModel>();
    }

    [RelayCommand]
    private void EditAvatar()
    {
        _navigationService.NavigateTo<ProfileEditorViewModel>();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenManager.LogoutAsync();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
