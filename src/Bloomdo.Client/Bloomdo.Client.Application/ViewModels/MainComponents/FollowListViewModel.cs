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
    private List<FollowStatusDto> _allUsers = [];
    private bool? _sortDirection;

    [ObservableProperty]
    private string _title = "Followers";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasUsers;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public string SortButtonText => _sortDirection switch
    {
        true => "A \u2192 Z",
        false => "Z \u2192 A",
        _ => "Sort"
    };

    public ObservableCollection<FollowStatusDto> Users { get; } = [];

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

            _allUsers = [..users];
            ApplyFilterAndSort();
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
    private void OpenProfile(FollowStatusDto? item)
    {
        if (item == null) return;
        _navigationService.NavigateTo<UserProfileViewModel>(vm => vm.Initialize(item.User.Id));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilterAndSort();

    [RelayCommand]
    private void ToggleSort()
    {
        _sortDirection = _sortDirection switch
        {
            null => true,
            true => false,
            false => null
        };
        OnPropertyChanged(nameof(SortButtonText));
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var filtered = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(u =>
                u.User.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(u.User.FirstName) && u.User.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.User.LastName) && u.User.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        filtered = _sortDirection switch
        {
            true => filtered.OrderBy(u => u.User.Username, StringComparer.OrdinalIgnoreCase),
            false => filtered.OrderByDescending(u => u.User.Username, StringComparer.OrdinalIgnoreCase),
            _ => filtered
        };

        Users.Clear();
        foreach (var u in filtered)
            Users.Add(u);

        HasUsers = Users.Count > 0;
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();
}
