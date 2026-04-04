using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class UserSearchViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<FollowStatusDto> SearchResults { get; } = [];

    public UserSearchViewModel(
        ISocialApiService socialApiService,
        INavigationService navigationService,
        IToastService toastService)
    {
        _socialApiService = socialApiService;
        _navigationService = navigationService;
        _toastService = toastService;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        IsLoading = true;
        try
        {
            var results = await _socialApiService.SearchUsersAsync(SearchQuery);
            SearchResults.Clear();
            foreach (var r in results)
                SearchResults.Add(r);
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
            await SearchAsync();
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
            await SearchAsync();
        }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();
}
