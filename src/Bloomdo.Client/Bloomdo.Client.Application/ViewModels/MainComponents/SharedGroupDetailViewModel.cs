using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Friends;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class SharedGroupDetailViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly IDailyActivityApiService _activityApiService;
    private readonly ISignalRClientService _signalR;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly IConfirmDialogService _confirmDialogService;

    private Guid _groupId;

    [ObservableProperty]
    private string _groupTitle = string.Empty;

    [ObservableProperty]
    private string _groupIcon = "📋";

    [ObservableProperty]
    private string _groupColor = "#7E57C2";

    [ObservableProperty]
    private bool _isOwner;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<DailyActivityItemDto> Items { get; } = [];
    public ObservableCollection<GroupMembershipDto> Members { get; } = [];
    public ObservableCollection<GroupMemberProgressDto> MemberProgresses { get; } = [];

    public SharedGroupDetailViewModel(
        ISocialApiService socialApiService,
        IDailyActivityApiService activityApiService,
        ISignalRClientService signalR,
        INavigationService navigationService,
        IToastService toastService,
        IConfirmDialogService confirmDialogService)
    {
        _socialApiService = socialApiService;
        _activityApiService = activityApiService;
        _signalR = signalR;
        _navigationService = navigationService;
        _toastService = toastService;
        _confirmDialogService = confirmDialogService;
    }

    public void Initialize(Guid groupId)
    {
        _groupId = groupId;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        SubscribeSignalR();
        _ = LoadAsync();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        UnsubscribeSignalR();
        _ = _signalR.LeaveGroupAsync(_groupId);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var detail = await _socialApiService.GetSharedGroupDetailAsync(_groupId);
            if (detail == null) { _navigationService.NavigateBack(); return; }

            GroupTitle = detail.Title;
            GroupIcon = detail.Icon;
            GroupColor = detail.Color;
            IsOwner = detail.IsOwner;

            Items.Clear();
            foreach (var item in detail.Items) Items.Add(item);

            Members.Clear();
            foreach (var m in detail.Members) Members.Add(m);

            MemberProgresses.Clear();
            foreach (var p in detail.MemberProgresses) MemberProgresses.Add(p);

            await _signalR.JoinGroupAsync(_groupId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTaskAsync(DailyActivityItemDto? item)
    {
        if (item == null) return;

        await _activityApiService.ToggleCompletionAsync(new ToggleCompletionRequest { ActivityItemId = item.Id, Date = DateOnly.FromDateTime(DateTime.Today) });
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(DailyActivityItemDto? item)
    {
        if (item == null || !IsOwner) return;

        var confirmed = await _confirmDialogService.ConfirmAsync(
            "Delete Task",
            $"Remove \"{item.Title}\" from this group?");
        if (!confirmed) return;

        var result = await _activityApiService.DeleteItemAsync(item.Id);
        if (result) await LoadAsync();
        else _toastService.ShowError("Could not delete task.");
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();

    [RelayCommand]
    private void OpenEditor()
    {
        _navigationService.NavigateTo<SharedGroupEditorViewModel>(vm => vm.Initialize(_groupId));
    }

    [RelayCommand]
    private void EditTask(DailyActivityItemDto? item)
    {
        if (item == null || !IsOwner) return;
        _navigationService.NavigateTo<SharedGroupEditorViewModel>(vm => vm.Initialize(_groupId));
    }

    private void SubscribeSignalR()
    {
        _signalR.TaskCompletedReceived += OnTaskCompleted;
        _signalR.NewGroupTaskReceived += OnNewGroupTask;
        _signalR.GroupDeletedReceived += OnGroupDeleted;
        _signalR.NewGroupMemberReceived += OnNewGroupMember;
    }

    private void UnsubscribeSignalR()
    {
        _signalR.TaskCompletedReceived -= OnTaskCompleted;
        _signalR.NewGroupTaskReceived -= OnNewGroupTask;
        _signalR.GroupDeletedReceived -= OnGroupDeleted;
        _signalR.NewGroupMemberReceived -= OnNewGroupMember;
    }

    private void OnTaskCompleted(Guid actorId, Guid itemId) => _ = LoadAsync();
    private void OnNewGroupTask(Guid itemId) => _ = LoadAsync();
    private void OnGroupDeleted(Guid groupId) { if (groupId == _groupId) _navigationService.NavigateBack(); }
    private void OnNewGroupMember(Guid groupId, ProfileSummaryDto member) { if (groupId == _groupId) _ = LoadAsync(); }
}
