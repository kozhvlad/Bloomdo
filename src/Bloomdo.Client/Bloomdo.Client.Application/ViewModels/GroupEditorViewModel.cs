using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Subscription;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class GroupEditorViewModel : PageViewModel
{
    private readonly IDailyActivityApiService? _activityApi;
    private readonly INavigationService _navigationService;
    private readonly IToastService? _toastService;
    private readonly ISubscriptionApiService? _subscriptionApiService;
    private readonly IConfirmDialogService? _confirmDialogService;
    private readonly ILocalSubscriptionStore? _localSubscriptionStore;

    private Guid? _editingGroupId;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _pageTitle = "New Group";

    [ObservableProperty]
    private bool _canCustomizeEmoji;

    [ObservableProperty]
    private bool _canCustomizeColors;

    // --- Group fields ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(FirstLetter))]
    private string _groupTitle = string.Empty;

    [ObservableProperty]
    private string _groupDescription = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewIcon))]
    private string _groupIcon = "📋";

    [ObservableProperty]
    private string _groupColor = "#7E57C2";

    [ObservableProperty]
    private bool _isEmojiPickerOpen;

    [ObservableProperty]
    private bool _isSaving;

    // --- Tasks within group ---

    public ObservableCollection<ActivityTaskItemViewModel> Tasks { get; } = [];

    [ObservableProperty]
    private bool _isAddingTask;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private string? _newTaskDescription;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private int _newTaskDurationMinutes = 30;

    [ObservableProperty]
    private string _newTaskIcon = "✨";

    [ObservableProperty]
    private string _newTaskColor = "#7E57C2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private ActivityItemType _newTaskType = ActivityItemType.Timer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTaskPreviewSubtitle))]
    private int _newTaskTargetCount = 8;

    [ObservableProperty]
    private bool _isTaskEmojiPickerOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewTaskCustomVerification))]
    private VerificationTemplate _newTaskVerificationTemplate = VerificationTemplate.Workout;

    [ObservableProperty]
    private string? _newTaskCustomVerificationCriteria;

    public bool IsNewTaskTimer => NewTaskType == ActivityItemType.Timer;
    public bool IsNewTaskCount => NewTaskType == ActivityItemType.Count;
    public bool IsNewTaskSteps => NewTaskType == ActivityItemType.Steps;
    public bool IsNewTaskCheckbox => NewTaskType == ActivityItemType.Checkbox;
    public bool IsNewTaskPhoto => NewTaskType == ActivityItemType.PhotoVerification;
    public bool IsNewTaskCustomVerification => IsNewTaskPhoto
                                               && NewTaskVerificationTemplate == VerificationTemplate.Custom;

    public string NewTaskPreviewSubtitle => NewTaskType switch
    {
        ActivityItemType.Timer => $"{NewTaskDurationMinutes} min",
        ActivityItemType.Count => $"0 / {NewTaskTargetCount}",
        ActivityItemType.Steps => $"0 / {NewTaskTargetCount} steps",
        ActivityItemType.Checkbox => "Tap to complete",
        ActivityItemType.PhotoVerification => "📷 Photo required",
        _ => string.Empty
    };

    // --- Computed ---

    public string PreviewTitle => string.IsNullOrWhiteSpace(GroupTitle) ? "Group Preview" : GroupTitle;
    public string PreviewIcon => string.IsNullOrWhiteSpace(GroupIcon) ? "📋" : GroupIcon;
    public string FirstLetter => string.IsNullOrEmpty(GroupTitle) ? "?" : GroupTitle[..1].ToUpperInvariant();

    public bool HasTasks => Tasks.Count > 0;

    public static string[] AvailableColors { get; } =
        ["#7E57C2", "#42A5F5", "#66BB6A", "#FF9800", "#EF5350", "#26C6DA", "#AB47BC", "#5C6BC0", "#EC407A", "#8D6E63",
         "#78909C", "#4DB6AC", "#FFB74D", "#F06292", "#9575CD", "#4FC3F7", "#81C784", "#DCE775", "#FF8A65", "#A1887F"];

    public static string[] AvailableEmojis { get; } =
    [
        "📋", "📚", "💪", "🏃", "🧘", "🎯", "🎨", "🎵", "💻", "📖",
        "✏️", "🔬", "🧠", "💡", "🌟", "🏆", "❤️", "🍎", "💧", "😴",
        "🧹", "🛒", "📝", "📞", "✈️", "🏠", "🌱", "🐾", "☕", "🎮",
        "📸", "🎭", "🎤", "🎸", "🏊", "🚴", "⚽", "🎾", "🧩", "♟️",
        "🍳", "🥗", "💊", "🧪", "📊", "💼", "🤝", "📌", "⏰", "🔔",
        "🚀", "🌈", "🔥", "⭐", "✨", "💎", "🎪", "🎬", "🎹", "🥊",
        "🏋️", "🤸", "⛷️", "🏄", "🧗", "🚶", "🛌", "🧑‍💻", "📱", "🖥️",
        "⌨️", "🎧", "📻", "📺", "🔑", "🏡", "🌍", "🌙", "☀️", "🌊",
        "🍃", "🌺", "🌻", "🍕", "🍔", "🥤", "🍰", "🍩", "🥑", "🫖",
        "🧃", "💰", "📈", "🗂️", "📎", "🔒", "💬", "✅", "🎁", "🧸",
        "🐶", "🐱", "🦋", "🐝", "🌵", "🍀", "🎂", "🎉", "🎊", "🪴",
        "🏅", "🥇", "👑", "💫", "⚡", "🔮", "🧲", "🎲", "🃏", "🧶"
    ];

    public GroupEditorViewModel(
        IDailyActivityApiService? activityApi,
        INavigationService navigationService,
        IToastService? toastService = null,
        ISubscriptionApiService? subscriptionApiService = null,
        IConfirmDialogService? confirmDialogService = null,
        ILocalSubscriptionStore? localSubscriptionStore = null)
    {
        _activityApi = activityApi;
        _navigationService = navigationService;
        _toastService = toastService;
        _subscriptionApiService = subscriptionApiService;
        _confirmDialogService = confirmDialogService;
        _localSubscriptionStore = localSubscriptionStore;
        _ = LoadCustomizationPermissionsAsync();
    }

    private async Task LoadCustomizationPermissionsAsync()
    {
        SubscriptionStatusResponse? status = null;

        if (_subscriptionApiService is not null)
        {
            try
            {
                status = await _subscriptionApiService.GetStatusAsync();
                if (status is not null && _localSubscriptionStore is not null)
                    _ = _localSubscriptionStore.SaveAsync(status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCustomizationPermissions error: {ex}");
            }
        }

        status ??= _localSubscriptionStore is not null ? await _localSubscriptionStore.LoadAsync() : null;

        if (status?.Limits is not null)
        {
            CanCustomizeEmoji = status.Limits.CanCustomizeEmoji;
            CanCustomizeColors = status.Limits.CanCustomizeColors;
        }
    }

    public void ConfigureForCreate()
    {
        _editingGroupId = null;
        IsEditMode = false;
        PageTitle = "New Group";
        GroupTitle = string.Empty;
        GroupDescription = string.Empty;
        GroupIcon = "📋";
        GroupColor = AvailableColors[new Random().Next(AvailableColors.Length)];
        Tasks.Clear();
        OnPropertyChanged(nameof(HasTasks));
    }

    public void ConfigureForEdit(ActivityGroupItemViewModel group)
    {
        _editingGroupId = group.Id;
        IsEditMode = true;
        PageTitle = "Edit Group";
        GroupTitle = group.Title;
        GroupIcon = group.Icon;
        GroupColor = group.Color;
        GroupDescription = string.Empty;

        Tasks.Clear();
        foreach (var task in group.Tasks)
        {
            Tasks.Add(new ActivityTaskItemViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                TaskType = task.TaskType,
                DurationMinutes = task.DurationMinutes,
                TargetCount = task.TargetCount,
                CurrentCount = task.CurrentCount,
                Icon = task.Icon,
                Color = task.Color,
                CurrentStreak = task.CurrentStreak,
                IsCompleted = task.IsCompleted
            });
        }
        OnPropertyChanged(nameof(HasTasks));
    }

    // --- Emoji picker ---

    [RelayCommand]
    private void ToggleEmojiPicker()
    {
        IsEmojiPickerOpen = !IsEmojiPickerOpen;
        if (IsEmojiPickerOpen) IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private async Task SelectGroupEmoji(string emoji)
    {
        if (!CanCustomizeEmoji)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        GroupIcon = emoji;
        IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void ToggleTaskEmojiPicker()
    {
        IsTaskEmojiPickerOpen = !IsTaskEmojiPickerOpen;
        if (IsTaskEmojiPickerOpen) IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    private async Task SelectTaskEmoji(string emoji)
    {
        if (!CanCustomizeEmoji)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        NewTaskIcon = emoji;
        IsTaskEmojiPickerOpen = false;
    }

    // --- Task type ---

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
        OnPropertyChanged(nameof(IsNewTaskCheckbox));
        OnPropertyChanged(nameof(IsNewTaskPhoto));
    }

    [RelayCommand]
    private void SelectNewTaskVerificationTemplate(string template)
    {
        if (Enum.TryParse<VerificationTemplate>(template, out var parsed))
            NewTaskVerificationTemplate = parsed;
    }

    [RelayCommand]
    private void IncrementTargetCount() => NewTaskTargetCount = Math.Min(NewTaskTargetCount + 1, 999);

    [RelayCommand]
    private void DecrementTargetCount() => NewTaskTargetCount = Math.Max(NewTaskTargetCount - 1, 1);

    [RelayCommand]
    private void IncrementTargetSteps() => NewTaskTargetCount = Math.Min(NewTaskTargetCount + 1000, 100000);

    [RelayCommand]
    private void DecrementTargetSteps() => NewTaskTargetCount = Math.Max(NewTaskTargetCount - 1000, 1000);

    // --- Color ---

    [RelayCommand]
    private async Task SelectGroupColor(string color)
    {
        if (!CanCustomizeColors)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        GroupColor = color;
    }

    [RelayCommand]
    private async Task SelectTaskColor(string color)
    {
        if (!CanCustomizeColors)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        NewTaskColor = color;
    }

    // --- Task duration ---

    [RelayCommand]
    private void IncrementDuration()
    {
        NewTaskDurationMinutes = Math.Min(NewTaskDurationMinutes + 5, 480);
    }

    [RelayCommand]
    private void DecrementDuration()
    {
        NewTaskDurationMinutes = Math.Max(NewTaskDurationMinutes - 5, 5);
    }

    // --- Task CRUD ---

    [RelayCommand]
    private void ShowAddTask()
    {
        NewTaskTitle = string.Empty;
        NewTaskDescription = null;
        NewTaskType = ActivityItemType.Timer;
        NewTaskDurationMinutes = 30;
        NewTaskTargetCount = 8;
        NewTaskIcon = "✨";
        NewTaskColor = GroupColor;
        NewTaskVerificationTemplate = VerificationTemplate.Workout;
        NewTaskCustomVerificationCriteria = null;
        IsAddingTask = true;
        IsTaskEmojiPickerOpen = false;
        OnPropertyChanged(nameof(IsNewTaskTimer));
        OnPropertyChanged(nameof(IsNewTaskCount));
        OnPropertyChanged(nameof(IsNewTaskSteps));
        OnPropertyChanged(nameof(IsNewTaskCheckbox));
        OnPropertyChanged(nameof(IsNewTaskPhoto));
    }

    [RelayCommand]
    private void CancelAddTask()
    {
        IsAddingTask = false;
        IsTaskEmojiPickerOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmAddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

        if (_editingGroupId.HasValue && _activityApi is not null)
        {
            var request = new CreateActivityItemRequest
            {
                ActivityGroupId = _editingGroupId.Value,
                Title = NewTaskTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(NewTaskDescription) ? null : NewTaskDescription.Trim(),
                TaskType = NewTaskType,
                DurationMinutes = IsNewTaskTimer ? NewTaskDurationMinutes : null,
                TargetCount = (IsNewTaskCount || IsNewTaskSteps) ? NewTaskTargetCount : null,
                Icon = NewTaskIcon,
                Color = NewTaskColor,
                VerificationTemplate = IsNewTaskPhoto ? NewTaskVerificationTemplate : null,
                CustomVerificationCriteria = IsNewTaskCustomVerification ? NewTaskCustomVerificationCriteria : null
            };

            var result = await _activityApi.CreateItemAsync(request);
            if (result is not null)
            {
                Tasks.Add(new ActivityTaskItemViewModel
                {
                    Id = result.Id,
                    Title = result.Title,
                    Description = result.Description,
                    TaskType = result.TaskType,
                    DurationMinutes = result.DurationMinutes,
                    TargetCount = result.TargetCount,
                    Icon = result.Icon,
                    Color = result.Color
                });
            }
        }
        else
        {
            Tasks.Add(new ActivityTaskItemViewModel
            {
                Id = Guid.NewGuid(),
                Title = NewTaskTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(NewTaskDescription) ? null : NewTaskDescription.Trim(),
                TaskType = NewTaskType,
                DurationMinutes = IsNewTaskTimer ? NewTaskDurationMinutes : null,
                TargetCount = (IsNewTaskCount || IsNewTaskSteps) ? NewTaskTargetCount : null,
                Icon = NewTaskIcon,
                Color = NewTaskColor,
                VerificationTemplate = IsNewTaskPhoto ? NewTaskVerificationTemplate : null,
                CustomVerificationCriteria = IsNewTaskCustomVerification ? NewTaskCustomVerificationCriteria : null
            });
        }

        IsAddingTask = false;
        IsTaskEmojiPickerOpen = false;
        OnPropertyChanged(nameof(HasTasks));
    }

    [RelayCommand]
    private async Task RemoveTask(ActivityTaskItemViewModel? task)
    {
        if (task is null) return;

        if (_confirmDialogService is not null)
        {
            var confirmed = await _confirmDialogService.ConfirmAsync(
                "Delete Task",
                $"Delete \"{task.Title}\"? This action cannot be undone.");
            if (!confirmed) return;
        }

        if (_editingGroupId.HasValue && _activityApi is not null)
        {
            await _activityApi.DeleteItemAsync(task.Id);
        }

        Tasks.Remove(task);
        OnPropertyChanged(nameof(HasTasks));
    }

    [RelayCommand]
    private void EditTask(ActivityTaskItemViewModel? task)
    {
        if (task is null) return;
        _navigationService.NavigateTo<TaskEditorViewModel>(vm =>
            vm.ConfigureForEdit(task, () => _navigationService.NavigateTo<GroupEditorViewModel>()));
    }

    // --- Save / Cancel ---

    [RelayCommand]
    private async Task SaveGroup()
    {
        if (string.IsNullOrWhiteSpace(GroupTitle) || _activityApi is null) return;

        IsSaving = true;
        try
        {
            if (IsEditMode && _editingGroupId.HasValue)
            {
                var updateReq = new UpdateActivityGroupRequest
                {
                    Title = GroupTitle.Trim(),
                    Icon = GroupIcon,
                    Color = GroupColor
                };

                await _activityApi.UpdateGroupAsync(_editingGroupId.Value, updateReq);
                _toastService?.ShowSuccess("Group updated!");
            }
            else
            {
                var createReq = new CreateActivityGroupRequest
                {
                    Title = GroupTitle.Trim(),
                    Icon = GroupIcon,
                    Color = GroupColor
                };

                var group = await _activityApi.CreateGroupAsync(createReq);
                if (group is not null)
                {
                    foreach (var task in Tasks)
                    {
                        var itemReq = new CreateActivityItemRequest
                        {
                            ActivityGroupId = group.Id,
                            Title = task.Title,
                            Description = task.Description,
                            TaskType = task.TaskType,
                            DurationMinutes = task.IsTimerType ? task.DurationMinutes : null,
                            TargetCount = (task.IsCountType || task.IsStepsType) ? task.TargetCount : null,
                            Icon = task.Icon,
                            Color = task.Color,
                            VerificationTemplate = task.IsPhotoVerificationType ? task.VerificationTemplate : null,
                            CustomVerificationCriteria = task.IsPhotoVerificationType ? task.CustomVerificationCriteria : null
                        };
                        await _activityApi.CreateItemAsync(itemReq);
                    }

                    _toastService?.ShowSuccess("Group created!");
                }
            }

            _navigationService.NavigateTo<MainComponents.MainViewModel>();
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateTo<MainComponents.MainViewModel>();
    }

    private async Task PromptPremiumUpgradeAsync()
    {
        if (_confirmDialogService is null) return;

        var upgrade = await _confirmDialogService.ConfirmAsync(
            "Bloomdo Plus",
            "Custom icons and colors are available with Bloomdo Plus. Would you like to subscribe?",
            "Subscribe",
            "Not now");

        if (upgrade)
        {
            _navigationService.NavigateTo<MainComponents.MainViewModel>(vm => vm.SelectedTabIndex = 5);
        }
    }
}
