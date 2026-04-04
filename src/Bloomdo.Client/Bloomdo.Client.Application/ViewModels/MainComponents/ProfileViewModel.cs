using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class ProfileViewModel : PageViewModel
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly IProfileApiService _profileApiService;
    private readonly ISocialApiService _socialApiService;

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
    private int _avatarBodyType;

    [ObservableProperty]
    private int _avatarHairStyle;

    [ObservableProperty]
    private int _avatarEyeStyle;

    [ObservableProperty]
    private int _avatarClothingStyle;

    [ObservableProperty]
    private int _avatarGlassesStyle;

    [ObservableProperty]
    private string _avatarGlassesHex = "#263238";

    [ObservableProperty]
    private int _avatarFacialHair;

    [ObservableProperty]
    private string _avatarFacialHairHex = "#2C2C2C";

    [ObservableProperty]
    private int _avatarHeadwearStyle;

    [ObservableProperty]
    private string _avatarHeadwearHex = "#EF5350";

    [ObservableProperty]
    private string _avatarEyeHex = "#5D4037";

    [ObservableProperty]
    private int _avatarMouthStyle;

    [ObservableProperty]
    private int _avatarFaceExtra;

    [ObservableProperty]
    private int _totalBlocksCreated;

    [ObservableProperty]
    private int _achievementsUnlocked;

    [ObservableProperty]
    private int _followersCount;

    [ObservableProperty]
    private int _followingCount;

    public ProfileViewModel(
        IAccessTokenManager tokenManager,
        INavigationService navigationService,
        IProfileApiService profileApiService,
        ISocialApiService socialApiService)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _profileApiService = profileApiService;
        _socialApiService = socialApiService;
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

            // Load followers/following counts
            var followers = await _socialApiService.GetFollowersAsync();
            var following = await _socialApiService.GetFollowingAsync();
            FollowersCount = followers.Count;
            FollowingCount = following.Count;

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
                    >= 100 => "Legend",
                    >= 50 => "Expert",
                    >= 30 => "Dedicated",
                    >= 7 => "Committed",
                    _ => "Beginner"
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
            AvatarEyeHex = GetEyeColor(avatar.EyeColor);
            AvatarBodyType = avatar.BodyType;
            AvatarHairStyle = avatar.HairStyle;
            AvatarEyeStyle = avatar.EyeStyle;
            AvatarClothingStyle = avatar.ClothingStyle;
            AvatarGlassesStyle = avatar.GlassesStyle;
            AvatarGlassesHex = GetGlassesColor(avatar.GlassesColor);
            AvatarFacialHair = avatar.FacialHair;
            AvatarFacialHairHex = GetFacialHairColor(avatar.FacialHairColor);
            AvatarHeadwearStyle = avatar.HeadwearStyle;
            AvatarHeadwearHex = GetHeadwearColor(avatar.HeadwearColor);
            AvatarMouthStyle = avatar.MouthStyle;
            AvatarFaceExtra = avatar.FaceExtra;
        }
        else
        {
            AvatarBackgroundHex = "#7E57C2";
            AvatarSkinHex = "#FDDBB4";
            AvatarHairHex = "#2C2C2C";
            AvatarClothingHex = "#66BB6A";
            AvatarEyeHex = "#5D4037";
            AvatarBodyType = 0;
            AvatarHairStyle = 0;
            AvatarEyeStyle = 0;
            AvatarClothingStyle = 0;
            AvatarGlassesStyle = 0;
            AvatarGlassesHex = "#263238";
            AvatarFacialHair = 0;
            AvatarFacialHairHex = "#2C2C2C";
            AvatarHeadwearStyle = 0;
            AvatarHeadwearHex = "#EF5350";
            AvatarMouthStyle = 0;
            AvatarFaceExtra = 0;
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
        6 => "#EF5350",
        7 => "#FDD835",
        8 => "#283593",
        9 => "#FF7043",
        10 => "#80CBC4",
        11 => "#B39DDB",
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
        6 => "#3A1F04",
        _ => "#FDDBB4"
    };

    private static string GetHairColor(int id) => id switch
    {
        0 => "#2C2C2C",
        1 => "#4E342E",
        2 => "#6B4226",
        3 => "#F5D76E",
        4 => "#C0392B",
        5 => "#E65100",
        6 => "#2980B9",
        7 => "#8E44AD",
        8 => "#EC407A",
        9 => "#43A047",
        10 => "#B0BEC5",
        11 => "#F5F5DC",
        12 => "#00897B",
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
        6 => "#ECEFF1",
        7 => "#EC407A",
        8 => "#26A69A",
        9 => "#FDD835",
        10 => "#1A237E",
        11 => "#880E4F",
        _ => "#66BB6A"
    };

    private static string GetEyeColor(int id) => id switch
    {
        0 => "#5D4037",
        1 => "#8D6E63",
        2 => "#4CAF50",
        3 => "#42A5F5",
        4 => "#78909C",
        5 => "#FFA000",
        6 => "#7E57C2",
        7 => "#26A69A",
        8 => "#81D4FA",
        9 => "#2E7D32",
        _ => "#5D4037"
    };

    private static string GetGlassesColor(int id) => id switch
    {
        0 => "#263238",
        1 => "#5D4037",
        2 => "#FFB300",
        3 => "#90A4AE",
        4 => "#1565C0",
        5 => "#C62828",
        6 => "#EC407A",
        7 => "#2E7D32",
        8 => "#7E57C2",
        _ => "#263238"
    };

    private static string GetFacialHairColor(int id) => id switch
    {
        0 => "#2C2C2C",
        1 => "#4E342E",
        2 => "#6B4226",
        3 => "#F5D76E",
        4 => "#C0392B",
        5 => "#E65100",
        6 => "#9E9E9E",
        7 => "#ECEFF1",
        8 => "#8D4004",
        _ => "#2C2C2C"
    };

    private static string GetHeadwearColor(int id) => id switch
    {
        0 => "#EF5350",
        1 => "#42A5F5",
        2 => "#66BB6A",
        3 => "#AB47BC",
        4 => "#FFA726",
        5 => "#37474F",
        6 => "#EC407A",
        7 => "#FDD835",
        8 => "#26A69A",
        9 => "#ECEFF1",
        _ => "#EF5350"
    };

    [RelayCommand]
    private void OpenSearch()
    {
        _navigationService.NavigateTo<UserSearchViewModel>();
    }

    [RelayCommand]
    private void OpenNotifications()
    {
        _navigationService.NavigateTo<NotificationsViewModel>();
    }

    [RelayCommand]
    private void OpenFollowers()
    {
        _navigationService.NavigateTo<FollowListViewModel>(vm => vm.Initialize(FollowListMode.Followers));
    }

    [RelayCommand]
    private void OpenFollowing()
    {
        _navigationService.NavigateTo<FollowListViewModel>(vm => vm.Initialize(FollowListMode.Following));
    }

    [RelayCommand]
    private void EditProfile()
    {
        _navigationService.NavigateTo<ProfileEditorViewModel>();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
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
