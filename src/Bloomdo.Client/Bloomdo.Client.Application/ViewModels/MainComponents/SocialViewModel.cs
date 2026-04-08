using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class SocialViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly IConfirmDialogService _confirmDialogService;
    private readonly ISignalRClientService _signalR;
    private readonly IConnectivityService? _connectivityService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOffline;

    public ObservableCollection<SharedGroupDto> Groups { get; } = [];

    public bool HasNoGroups => Groups.Count == 0;

    public SocialViewModel(
        ISocialApiService socialApiService,
        INavigationService navigationService,
        IToastService toastService,
        IConfirmDialogService confirmDialogService,
        ISignalRClientService signalR,
        IConnectivityService? connectivityService = null)
    {
        _socialApiService = socialApiService;
        _navigationService = navigationService;
        _toastService = toastService;
        _confirmDialogService = confirmDialogService;
        _signalR = signalR;
        _connectivityService = connectivityService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        IsOffline = _connectivityService is not null && !_connectivityService.IsOnline;
        if (IsOffline) return;
        SubscribeSignalR();
        _ = LoadGroupsAsync();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        UnsubscribeSignalR();
    }

    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        IsLoading = true;
        try
        {
            var groups = await _socialApiService.GetSharedGroupsAsync();
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);

            OnPropertyChanged(nameof(HasNoGroups));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowCreatePanel()
    {
        _navigationService.NavigateTo<SharedGroupEditorViewModel>(vm => vm.ConfigureForCreate());
    }

    [RelayCommand]
    private void OpenGroupDetail(SharedGroupDto? group)
    {
        if (group == null) return;
        _navigationService.NavigateTo<SharedGroupDetailViewModel>(vm => vm.Initialize(group.Id));
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(SharedGroupDto? group)
    {
        if (group is null || !group.IsOwner) return;

        var confirmed = await _confirmDialogService.ConfirmAsync(
            "Delete Group",
            $"Are you sure you want to delete \"{group.Title}\"? This will remove it for all members.");
        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var result = await _socialApiService.DeleteSharedGroupAsync(group.Id);
            if (result)
            {
                Groups.Remove(group);
                OnPropertyChanged(nameof(HasNoGroups));
                _toastService.ShowSuccess("Group deleted.");
            }
            else
            {
                _toastService.ShowError("Could not delete group.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditGroup(SharedGroupDto? group)
    {
        if (group is null || !group.IsOwner) return;
        _navigationService.NavigateTo<SharedGroupEditorViewModel>(vm => vm.Initialize(group.Id));
    }

    private void SubscribeSignalR()
    {
        _signalR.GroupInviteReceived += OnGroupInviteReceived;
        _signalR.GroupDeletedReceived += OnGroupDeletedReceived;
    }

    private void UnsubscribeSignalR()
    {
        _signalR.GroupInviteReceived -= OnGroupInviteReceived;
        _signalR.GroupDeletedReceived -= OnGroupDeletedReceived;
    }

    private void OnGroupInviteReceived(SharedGroupDto group, Bloomdo.Shared.DTOs.Friends.ProfileSummaryDto inviter)
    {
        _toastService.ShowInfo($"@{inviter.Username} invited you to \"{group.Title}\"");
        _ = LoadGroupsAsync();
    }

    private void OnGroupDeletedReceived(Guid groupId)
    {
        var existing = Groups.FirstOrDefault(g => g.Id == groupId);
        if (existing != null) Groups.Remove(existing);
        OnPropertyChanged(nameof(HasNoGroups));
    }
}
