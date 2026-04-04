using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public enum FollowListMode
{
    Followers,
    Following
}

public partial class FollowListViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;

    private FollowListMode _mode;

    [ObservableProperty]
    private string _title = "Followers";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<FollowStatusDto> Users { get; } = [];

    public bool HasUsers => Users.Count > 0;

    public FollowListViewModel(
        ISocialApiService socialApiService,
        INavigationService navigationService,
        IToastService toastService)
    {
        _socialApiService = socialApiService;
        _navigationService = navigationService;
        _toastService = toastService;
    }

    public void Initialize(FollowListMode mode)
    {
        _mode = mode;
        Title = mode == FollowListMode.Followers ? "Followers" : "Following";
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var users = _mode == FollowListMode.Followers
                ? await _socialApiService.GetFollowersAsync()
                : await _socialApiService.GetFollowingAsync();

            Users.Clear();
            foreach (var u in users)
                Users.Add(u);

            OnPropertyChanged(nameof(HasUsers));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FollowAsync(FollowStatusDto? item)
    {
        if (item == null) return;

        var result = await _socialApiService.FollowUserAsync(item.User.Id);
        if (result)
        {
            var msg = item.IsPrivateProfile ? "Follow request sent!" : $"Now following @{item.User.Username}";
            _toastService.ShowSuccess(msg);
            await LoadAsync();
        }
        else
        {
            _toastService.ShowError("Could not follow user.");
        }
    }

    [RelayCommand]
    private async Task UnfollowAsync(FollowStatusDto? item)
    {
        if (item == null) return;

        var result = await _socialApiService.UnfollowUserAsync(item.User.Id);
        if (result)
        {
            _toastService.ShowInfo($"Unfollowed @{item.User.Username}");
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();
}
