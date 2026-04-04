using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class SharedGroupEditorViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly IDailyActivityApiService _activityApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly IConfirmDialogService _confirmDialogService;
    private readonly ISubscriptionApiService? _subscriptionApiService;

    private Guid _groupId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCreateMode;

    [ObservableProperty]
    private string _pageTitle = "Edit Group";

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editIcon = "\ud83d\udccb";

    [ObservableProperty]
    private string _editColor = "#7E57C2";

    [ObservableProperty]
    private bool _isOwner;

    public bool IsEditMode => !IsCreateMode;
    public string SaveButtonText => IsCreateMode ? "Create" : "Save";

    [ObservableProperty]
    private bool _isInviteSectionVisible;

    [ObservableProperty]
    private bool _isEmojiPickerOpen;

    [ObservableProperty]
    private bool _canCustomizeEmoji = true;

    [ObservableProperty]
    private bool _canCustomizeColors = true;

    // --- Add Task fields ---

    [ObservableProperty]
    private bool _isAddingTask;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private string? _newTaskDescription;

    [ObservableProperty]
    private string _newTaskIcon = "\u2728";

    [ObservableProperty]
    private string _newTaskColor = "#7E57C2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private ActivityItemType _newTaskType = ActivityItemType.Checkbox;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private int _newTaskDurationMinutes = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private int _newTaskTargetCount = 8;

    [ObservableProperty]
    private bool _isTaskEmojiPickerOpen;

    public bool IsNewTaskTimer => NewTaskType == ActivityItemType.Timer;
    public bool IsNewTaskCount => NewTaskType == ActivityItemType.Count;
    public bool IsNewTaskSteps => NewTaskType == ActivityItemType.Steps;

    public string NewTaskPreviewSubtitle => NewTaskType switch
    {
        ActivityItemType.Timer => $"{NewTaskDurationMinutes} min",
        ActivityItemType.Count => $"0 / {NewTaskTargetCount}",
        ActivityItemType.Steps => $"0 / {NewTaskTargetCount} steps",
        ActivityItemType.Checkbox => "Tap to complete",
        _ => string.Empty
    };

    public ObservableCollection<GroupMembershipDto> Members { get; } = [];
    public ObservableCollection<DailyActivityItemDto> Tasks { get; } = [];
    public ObservableCollection<ProfileSummaryDto> MutualFollowers { get; } = [];
    public ObservableCollection<ProfileSummaryDto> PendingInvites { get; } = [];

    public bool HasPendingInvites => PendingInvites.Count > 0;

    public static string[] AvailableColors { get; } =
        ["#7E57C2", "#42A5F5", "#66BB6A", "#FF9800", "#EF5350", "#26C6DA", "#AB47BC", "#5C6BC0", "#EC407A", "#8D6E63",
         "#78909C", "#4DB6AC", "#FFB74D", "#F06292", "#9575CD", "#4FC3F7", "#81C784", "#DCE775", "#FF8A65", "#A1887F"];

    public static string[] AvailableEmojis { get; } =
    [
        "\ud83d\udccb", "\ud83d\udcda", "\ud83d\udcaa", "\ud83c\udfc3", "\ud83e\uddd8", "\ud83c\udfaf", "\ud83c\udfa8", "\ud83c\udfb5", "\ud83d\udcbb", "\ud83d\udcd6",
        "\u270f\ufe0f", "\ud83d\udd2c", "\ud83e\udde0", "\ud83d\udca1", "\ud83c\udf1f", "\ud83c\udfc6", "\u2764\ufe0f", "\ud83c\udf4e", "\ud83d\udca7", "\ud83d\ude34",
        "\ud83e\uddf9", "\ud83d\uded2", "\ud83d\udcdd", "\ud83d\udcde", "\u2708\ufe0f", "\ud83c\udfe0", "\ud83c\udf31", "\ud83d\udc3e", "\u2615", "\ud83c\udfae",
        "\ud83d\udcf8", "\ud83c\udfad", "\ud83c\udfa4", "\ud83c\udfb8", "\ud83c\udfca", "\ud83d\udeb4", "\u26bd", "\ud83c\udfbe", "\ud83e\udde9", "\u265f\ufe0f",
        "\ud83c\udf73", "\ud83e\udd57", "\ud83d\udc8a", "\ud83e\uddea", "\ud83d\udcca", "\ud83d\udcbc", "\ud83e\udd1d", "\ud83d\udccc", "\u23f0", "\ud83d\udd14",
        "\ud83d\ude80", "\ud83c\udf08", "\ud83d\udd25", "\u2b50", "\u2728", "\ud83d\udc8e", "\ud83c\udfea", "\ud83c\udfac", "\ud83c\udfb9", "\ud83e\udd4a",
        "\ud83c\udfcb\ufe0f", "\ud83e\udd38", "\u26f7\ufe0f", "\ud83c\udfc4", "\ud83e\uddd7", "\ud83d\udeb6", "\ud83d\udecc", "\ud83e\uddd1\u200d\ud83d\udcbb", "\ud83d\udcf1", "\ud83d\udda5\ufe0f"
    ];

    public SharedGroupEditorViewModel(
        ISocialApiService socialApiService,
        IDailyActivityApiService activityApiService,
        INavigationService navigationService,
        IToastService toastService,
        IConfirmDialogService confirmDialogService,
        ISubscriptionApiService? subscriptionApiService = null)
    {
        _socialApiService = socialApiService;
        _activityApiService = activityApiService;
        _navigationService = navigationService;
        _toastService = toastService;
        _confirmDialogService = confirmDialogService;
        _subscriptionApiService = subscriptionApiService;
        _ = LoadCustomizationPermissionsAsync();
    }

    public void Initialize(Guid groupId)
    {
        _groupId = groupId;
        IsCreateMode = false;
        PageTitle = "Edit Group";
    }

    public void ConfigureForCreate()
    {
        _groupId = Guid.Empty;
        IsCreateMode = true;
        PageTitle = "New Shared Group";
        EditTitle = string.Empty;
        EditIcon = "\ud83d\udccb";
        EditColor = AvailableColors[new Random().Next(AvailableColors.Length)];
        IsOwner = true;
        Tasks.Clear();
        Members.Clear();
        MutualFollowers.Clear();
        PendingInvites.Clear();
        IsInviteSectionVisible = false;
        IsAddingTask = false;
        IsEmojiPickerOpen = false;
        IsTaskEmojiPickerOpen = false;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(HasPendingInvites));
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        if (!IsCreateMode)
            _ = LoadAsync();
    }

    private async Task LoadCustomizationPermissionsAsync()
    {
        if (_subscriptionApiService is null) return;
        try
        {
            var status = await _subscriptionApiService.GetStatusAsync();
            if (status?.Limits is not null)
            {
                CanCustomizeEmoji = status.Limits.CanCustomizeEmoji;
                CanCustomizeColors = status.Limits.CanCustomizeColors;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCustomizationPermissions error: {ex}");
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var detail = await _socialApiService.GetSharedGroupDetailAsync(_groupId);
            if (detail == null)
            {
                _navigationService.NavigateBack();
                return;
            }

            EditTitle = detail.Title;
            EditIcon = detail.Icon;
            EditColor = detail.Color;
            IsOwner = detail.IsOwner;

            Members.Clear();
            foreach (var m in detail.Members) Members.Add(m);

            Tasks.Clear();
            foreach (var t in detail.Items) Tasks.Add(t);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            _toastService.ShowError("Group name cannot be empty.");
            return;
        }

        IsLoading = true;
        try
        {
            if (IsCreateMode)
            {
                var group = await _socialApiService.CreateSharedGroupAsync(EditTitle.Trim(), EditIcon, EditColor);
                if (group != null)
                {
                    foreach (var task in Tasks)
                    {
                        var itemReq = new CreateActivityItemRequest
                        {
                            ActivityGroupId = group.Id,
                            Title = task.Title,
                            Description = task.Description,
                            TaskType = task.TaskType,
                            DurationMinutes = task.DurationMinutes,
                            TargetCount = task.TargetCount,
                            Icon = task.Icon,
                            Color = task.Color
                        };
                        await _activityApiService.CreateItemAsync(itemReq);
                    }

                    foreach (var invite in PendingInvites)
                    {
                        await _socialApiService.InviteToGroupAsync(group.Id, invite.Id);
                    }

                    _toastService.ShowSuccess($"Group \"{group.Title}\" created!");
                    _navigationService.NavigateBack();
                }
                else
                {
                    _toastService.ShowError("Could not create group.");
                }
            }
            else
            {
                var request = new UpdateSharedGroupRequest
                {
                    Title = EditTitle.Trim(),
                    Icon = EditIcon,
                    Color = EditColor
                };

                var result = await _socialApiService.UpdateSharedGroupAsync(_groupId, request);
                if (result != null)
                {
                    _toastService.ShowSuccess("Group updated!");
                    _navigationService.NavigateBack();
                }
                else
                {
                    _toastService.ShowError("Could not update group.");
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _navigationService.NavigateBack();

    // --- Emoji picker ---

    [RelayCommand]
    private void ToggleEmojiPicker()
    {
        IsEmojiPickerOpen = !IsEmojiPickerOpen;
        if (IsEmojiPickerOpen) IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void SelectEmoji(string emoji)
    {
        EditIcon = emoji;
        IsEmojiPickerOpen = false;
    }

    // --- Color ---

    [RelayCommand]
    private void SelectColor(string color) => EditColor = color;

    // --- Members ---

    [RelayCommand]
    private async Task RemoveMemberAsync(GroupMembershipDto? member)
    {
        if (member == null || !IsOwner) return;

        var confirmed = await _confirmDialogService.ConfirmAsync(
            "Remove Member",
            $"Remove @{member.Account.Username} from the group?");
        if (!confirmed) return;

        var result = await _socialApiService.RemoveMemberAsync(_groupId, member.Account.Id);
        if (result)
        {
            Members.Remove(member);
            _toastService.ShowSuccess($"@{member.Account.Username} removed.");
        }
    }

    [RelayCommand]
    private async Task ToggleInviteSectionAsync()
    {
        if (IsInviteSectionVisible)
        {
            IsInviteSectionVisible = false;
            return;
        }

        var mutual = await _socialApiService.GetMutualFollowersAsync();
        var memberIds = Members.Select(m => m.Account.Id).ToHashSet();
        var pendingIds = PendingInvites.Select(p => p.Id).ToHashSet();
        MutualFollowers.Clear();
        foreach (var u in mutual.Where(u => !memberIds.Contains(u.Id) && !pendingIds.Contains(u.Id)))
            MutualFollowers.Add(u);

        IsInviteSectionVisible = true;
    }

    [RelayCommand]
    private async Task InviteUserAsync(ProfileSummaryDto? user)
    {
        if (user == null) return;

        if (IsCreateMode)
        {
            PendingInvites.Add(user);
            MutualFollowers.Remove(user);
            OnPropertyChanged(nameof(HasPendingInvites));
            _toastService.ShowSuccess($"@{user.Username} will be invited!");
        }
        else
        {
            var result = await _socialApiService.InviteToGroupAsync(_groupId, user.Id);
            if (result)
            {
                MutualFollowers.Remove(user);
                _toastService.ShowSuccess($"Invited @{user.Username}!");
            }
            else
            {
                _toastService.ShowError("Could not invite this user.");
            }
        }
    }

    [RelayCommand]
    private void RemovePendingInvite(ProfileSummaryDto? user)
    {
        if (user == null) return;
        PendingInvites.Remove(user);
        OnPropertyChanged(nameof(HasPendingInvites));
    }

    // --- Task CRUD ---

    [RelayCommand]
    private async Task DeleteTaskAsync(DailyActivityItemDto? item)
    {
        if (item == null || !IsOwner) return;

        var confirmed = await _confirmDialogService.ConfirmAsync(
            "Delete Task",
            $"Remove \"{item.Title}\" from this group?");
        if (!confirmed) return;

        if (IsCreateMode)
        {
            Tasks.Remove(item);
            _toastService.ShowSuccess("Task removed.");
        }
        else
        {
            var result = await _activityApiService.DeleteItemAsync(item.Id);
            if (result)
            {
                Tasks.Remove(item);
                _toastService.ShowSuccess("Task removed.");
            }
            else
            {
                _toastService.ShowError("Could not delete task.");
            }
        }
    }

    [RelayCommand]
    private void ShowAddTask()
    {
        NewTaskTitle = string.Empty;
        NewTaskDescription = null;
        NewTaskType = ActivityItemType.Checkbox;
        NewTaskDurationMinutes = 30;
        NewTaskTargetCount = 8;
        NewTaskIcon = "\u2728";
        NewTaskColor = EditColor;
        IsAddingTask = true;
        IsTaskEmojiPickerOpen = false;
        OnPropertyChanged(nameof(IsNewTaskTimer));
        OnPropertyChanged(nameof(IsNewTaskCount));
        OnPropertyChanged(nameof(IsNewTaskSteps));
    }

    [RelayCommand]
    private void HideAddTask()
    {
        IsAddingTask = false;
        IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

        if (IsCreateMode)
        {
            Tasks.Add(new DailyActivityItemDto
            {
                Id = Guid.NewGuid(),
                Title = NewTaskTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(NewTaskDescription) ? null : NewTaskDescription.Trim(),
                TaskType = NewTaskType,
                DurationMinutes = IsNewTaskTimer ? NewTaskDurationMinutes : null,
                TargetCount = (IsNewTaskCount || IsNewTaskSteps) ? NewTaskTargetCount : null,
                Icon = NewTaskIcon,
                Color = NewTaskColor
            });
            _toastService.ShowSuccess("Task added!");
        }
        else
        {
            var request = new CreateActivityItemRequest
            {
                ActivityGroupId = _groupId,
                Title = NewTaskTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(NewTaskDescription) ? null : NewTaskDescription.Trim(),
                TaskType = NewTaskType,
                DurationMinutes = IsNewTaskTimer ? NewTaskDurationMinutes : null,
                TargetCount = (IsNewTaskCount || IsNewTaskSteps) ? NewTaskTargetCount : null,
                Icon = NewTaskIcon,
                Color = NewTaskColor
            };

            var result = await _activityApiService.CreateItemAsync(request);
            if (result is not null)
            {
                _toastService.ShowSuccess("Task added!");
                await LoadAsync();
            }
            else
            {
                _toastService.ShowError("Could not add task.");
            }
        }

        IsAddingTask = false;
        IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void SelectNewTaskType(string type)
    {
        NewTaskType = type switch
        {
            "Count" => ActivityItemType.Count,
            "Steps" => ActivityItemType.Steps,
            "Checkbox" => ActivityItemType.Checkbox,
            "Photo" => ActivityItemType.PhotoVerification,
            _ => ActivityItemType.Timer
        };
        if (NewTaskType == ActivityItemType.Steps)
            NewTaskTargetCount = 10000;
        else if (NewTaskType == ActivityItemType.Count)
            NewTaskTargetCount = 8;
        OnPropertyChanged(nameof(IsNewTaskTimer));
        OnPropertyChanged(nameof(IsNewTaskCount));
        OnPropertyChanged(nameof(IsNewTaskSteps));
    }

    [RelayCommand]
    private void ToggleTaskEmojiPicker()
    {
        IsTaskEmojiPickerOpen = !IsTaskEmojiPickerOpen;
        if (IsTaskEmojiPickerOpen) IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void SelectTaskEmoji(string emoji)
    {
        NewTaskIcon = emoji;
        IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void SelectTaskColor(string color) => NewTaskColor = color;

    [RelayCommand]
    private void IncrementDuration() => NewTaskDurationMinutes = Math.Min(NewTaskDurationMinutes + 5, 480);

    [RelayCommand]
    private void DecrementDuration() => NewTaskDurationMinutes = Math.Max(NewTaskDurationMinutes - 5, 5);

    [RelayCommand]
    private void IncrementTargetCount() => NewTaskTargetCount = Math.Min(NewTaskTargetCount + 1, 999);

    [RelayCommand]
    private void DecrementTargetCount() => NewTaskTargetCount = Math.Max(NewTaskTargetCount - 1, 1);

    [RelayCommand]
    private void IncrementTargetSteps() => NewTaskTargetCount = Math.Min(NewTaskTargetCount + 1000, 100000);

    [RelayCommand]
    private void DecrementTargetSteps() => NewTaskTargetCount = Math.Max(NewTaskTargetCount - 1000, 1000);

    // --- Delete Group ---

    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (!IsOwner) return;

        var confirmed = await _confirmDialogService.ConfirmAsync(
            "Delete Group",
            "This will permanently delete the group for all members.");
        if (!confirmed) return;

        var result = await _socialApiService.DeleteSharedGroupAsync(_groupId);
        if (result)
        {
            _toastService.ShowSuccess("Group deleted.");
            _navigationService.NavigateBack();
        }
        else
        {
            _toastService.ShowError("Could not delete group.");
        }
    }
}
