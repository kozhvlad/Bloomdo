using Bloomdo.Client.Application.Helpers;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;
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
    private readonly ISubscriptionApiService _subscriptionApiService;
    private readonly IConnectivityService? _connectivityService;
    private readonly ILocalProfileStore? _localProfileStore;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnreadNotifications))]
    private int _unreadNotificationCount;

    [ObservableProperty]
    private bool _isPremium;

    public bool HasUnreadNotifications => UnreadNotificationCount > 0;

    public ProfileViewModel(
        IAccessTokenManager tokenManager,
        INavigationService navigationService,
        IProfileApiService profileApiService,
        ISocialApiService socialApiService,
        ISubscriptionApiService subscriptionApiService,
        IConnectivityService? connectivityService = null,
        ILocalProfileStore? localProfileStore = null)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _profileApiService = profileApiService;
        _socialApiService = socialApiService;
        _subscriptionApiService = subscriptionApiService;
        _connectivityService = connectivityService;
        _localProfileStore = localProfileStore;
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

            var isOnline = _connectivityService?.IsOnline ?? true;
            if (!isOnline)
            {
                await RestoreFromLocalCacheAsync();
                return;
            }

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

                FollowersCount = profile.FollowersCount;
                FollowingCount = profile.FollowingCount;

                ApplyAvatar(profile.Avatar);
            }

            // Load unread notification count
            var notifications = await _socialApiService.GetNotificationsAsync();
            UnreadNotificationCount = notifications.Count(n => !n.IsRead);

            // Load subscription status
            var status = await _subscriptionApiService.GetStatusAsync();
            IsPremium = status?.IsPremium ?? false;

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

            // Persist snapshot for offline use
            await SaveToLocalCacheAsync();
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

    private async Task SaveToLocalCacheAsync()
    {
        if (_localProfileStore is null) return;
        try
        {
            var snapshot = new LocalProfileSnapshot
            {
                LastUpdatedUtc = DateTime.UtcNow,
                Name = Name,
                Email = Email,
                Username = Username,
                Bio = Bio,
                Initials = Initials,
                JoinedDateText = JoinedDateText,
                Avatar = CurrentAvatar,
                FollowersCount = FollowersCount,
                FollowingCount = FollowingCount,
                StreakDays = StreakDays,
                TasksCompleted = TasksCompleted,
                FocusHours = FocusHours,
                TotalBlocksCreated = TotalBlocksCreated,
                AchievementsUnlocked = AchievementsUnlocked,
                Level = Level,
                IsPremium = IsPremium
            };
            await _localProfileStore.SaveAsync(snapshot);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save profile cache failed: {ex.Message}");
        }
    }

    private async Task RestoreFromLocalCacheAsync()
    {
        if (_localProfileStore is null) return;
        try
        {
            var snapshot = await _localProfileStore.LoadAsync();
            if (snapshot is null) return;

            Name = snapshot.Name;
            Email = snapshot.Email;
            Username = snapshot.Username;
            Bio = snapshot.Bio;
            Initials = snapshot.Initials;
            JoinedDateText = snapshot.JoinedDateText;
            FollowersCount = snapshot.FollowersCount;
            FollowingCount = snapshot.FollowingCount;
            StreakDays = snapshot.StreakDays;
            TasksCompleted = snapshot.TasksCompleted;
            FocusHours = snapshot.FocusHours;
            TotalBlocksCreated = snapshot.TotalBlocksCreated;
            AchievementsUnlocked = snapshot.AchievementsUnlocked;
            Level = snapshot.Level;
            IsPremium = snapshot.IsPremium;
            ApplyAvatar(snapshot.Avatar);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restore profile cache failed: {ex.Message}");
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
            AvatarBackgroundHex = AvatarColorHelper.GetBackgroundColor(avatar.BackgroundColor);
            AvatarSkinHex = AvatarColorHelper.GetSkinColor(avatar.SkinTone);
            AvatarHairHex = AvatarColorHelper.GetHairColor(avatar.HairColor);
            AvatarClothingHex = AvatarColorHelper.GetClothingColor(avatar.ClothingColor);
            AvatarEyeHex = AvatarColorHelper.GetEyeColor(avatar.EyeColor);
            AvatarBodyType = avatar.BodyType;
            AvatarHairStyle = avatar.HairStyle;
            AvatarEyeStyle = avatar.EyeStyle;
            AvatarClothingStyle = avatar.ClothingStyle;
            AvatarGlassesStyle = avatar.GlassesStyle;
            AvatarGlassesHex = AvatarColorHelper.GetGlassesColor(avatar.GlassesColor);
            AvatarFacialHair = avatar.FacialHair;
            AvatarFacialHairHex = AvatarColorHelper.GetFacialHairColor(avatar.FacialHairColor);
            AvatarHeadwearStyle = avatar.HeadwearStyle;
            AvatarHeadwearHex = AvatarColorHelper.GetHeadwearColor(avatar.HeadwearColor);
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

    private static string GetBackgroundColor(int id) => AvatarColorHelper.GetBackgroundColor(id);
    private static string GetSkinColor(int id) => AvatarColorHelper.GetSkinColor(id);
    private static string GetHairColor(int id) => AvatarColorHelper.GetHairColor(id);
    private static string GetClothingColor(int id) => AvatarColorHelper.GetClothingColor(id);
    private static string GetEyeColor(int id) => AvatarColorHelper.GetEyeColor(id);
    private static string GetGlassesColor(int id) => AvatarColorHelper.GetGlassesColor(id);
    private static string GetFacialHairColor(int id) => AvatarColorHelper.GetFacialHairColor(id);
    private static string GetHeadwearColor(int id) => AvatarColorHelper.GetHeadwearColor(id);

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
