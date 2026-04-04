using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Social;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class NotificationsViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<NotificationDto> Notifications { get; } = [];

    public bool HasNotifications => Notifications.Count > 0;

    public NotificationsViewModel(
        ISocialApiService socialApiService,
        INavigationService navigationService)
    {
        _socialApiService = socialApiService;
        _navigationService = navigationService;
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
            var items = await _socialApiService.GetNotificationsAsync();
            Notifications.Clear();
            foreach (var n in items)
                Notifications.Add(n);

            OnPropertyChanged(nameof(HasNotifications));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MarkReadAsync(NotificationDto? notif)
    {
        if (notif == null || notif.IsRead) return;
        await _socialApiService.MarkNotificationReadAsync(notif.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AcceptFollowRequestAsync(NotificationDto? notif)
    {
        if (notif?.ReferenceId == null) return;
        await _socialApiService.RespondToFollowRequestAsync(notif.ReferenceId.Value, true);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AcceptGroupInviteAsync(NotificationDto? notif)
    {
        if (notif?.ReferenceId == null) return;
        await _socialApiService.RespondToGroupInviteAsync(notif.ReferenceId.Value, true);
        await LoadAsync();
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();

    public static string GetNotificationText(NotificationDto notif) => notif.Type switch
    {
        NotificationType.NewFollower => $"@{notif.Actor?.Username} started following you",
        NotificationType.FollowRequest => $"@{notif.Actor?.Username} wants to follow you",
        NotificationType.GroupInvite => $"@{notif.Actor?.Username} invited you to a group",
        NotificationType.GroupTaskCompleted => $"@{notif.Actor?.Username} completed a task",
        NotificationType.GroupNewTask => "A new task was added to your group",
        NotificationType.GroupDeleted => "A group you were in was deleted",
        _ => "New notification"
    };
}
