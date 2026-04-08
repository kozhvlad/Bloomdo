using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class TaskEditorViewModel : PageViewModel
{
    private readonly IDailyActivityApiService? _activityApi;
    private readonly INavigationService _navigationService;
    private readonly IToastService? _toastService;
    private readonly ISubscriptionApiService? _subscriptionApiService;
    private readonly IConfirmDialogService? _confirmDialogService;

    private Guid? _editingTaskId;
    private Guid _parentGroupId;
    private Action? _onComplete;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _pageTitle = "New Task";

    [ObservableProperty]
    private bool _canCustomizeEmoji;

    [ObservableProperty]
    private bool _canCustomizeColors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(FirstLetter))]
    private string _taskTitle = string.Empty;

    [ObservableProperty]
    private string? _taskDescription;

    [ObservableProperty]
    private ActivityItemType _selectedTaskType = ActivityItemType.Timer;

    [ObservableProperty]
    private int _taskDurationMinutes = 30;

    [ObservableProperty]
    private int _taskTargetCount = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewIcon))]
    private string _taskIcon = "✨";

    [ObservableProperty]
    private string _taskColor = "#7E57C2";

    [ObservableProperty]
    private bool _isEmojiPickerOpen;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomVerification))]
    private VerificationTemplate _selectedVerificationTemplate = VerificationTemplate.Workout;

    [ObservableProperty]
    private string? _customVerificationCriteria;

    public bool IsTimerType => SelectedTaskType == ActivityItemType.Timer;
    public bool IsCountType => SelectedTaskType == ActivityItemType.Count;
    public bool IsStepsType => SelectedTaskType == ActivityItemType.Steps;
    public bool IsCheckboxType => SelectedTaskType == ActivityItemType.Checkbox;
    public bool IsPhotoVerificationType => SelectedTaskType == ActivityItemType.PhotoVerification;
    public bool IsCustomVerification => SelectedTaskType == ActivityItemType.PhotoVerification
                                        && SelectedVerificationTemplate == VerificationTemplate.Custom;

    public string PreviewTitle => string.IsNullOrWhiteSpace(TaskTitle) ? "Task Preview" : TaskTitle;
    public string PreviewIcon => string.IsNullOrWhiteSpace(TaskIcon) ? "✨" : TaskIcon;
    public string FirstLetter => string.IsNullOrEmpty(TaskTitle) ? "?" : TaskTitle[..1].ToUpperInvariant();
    public string DurationLabel => $"{TaskDurationMinutes} min";
    public string PreviewSubtitle => SelectedTaskType switch
    {
        ActivityItemType.Timer => $"{TaskDurationMinutes} min",
        ActivityItemType.Count => $"0/{TaskTargetCount}",
        ActivityItemType.Steps => $"0/{TaskTargetCount} steps",
        ActivityItemType.Checkbox => "Tap to complete",
        ActivityItemType.PhotoVerification => "Tap to verify with photo",
        _ => string.Empty
    };

    public static string[] AvailableColors { get; } =
        ["#7E57C2", "#42A5F5", "#66BB6A", "#FF9800", "#EF5350", "#26C6DA", "#AB47BC", "#5C6BC0", "#EC407A", "#8D6E63",
         "#78909C", "#4DB6AC", "#FFB74D", "#F06292", "#9575CD", "#4FC3F7", "#81C784", "#DCE775", "#FF8A65", "#A1887F"];

    public static string[] AvailableEmojis { get; } =
    [
        "✨", "📖", "💪", "🏃", "🧘", "🎯", "✏️", "💻", "📝", "📞",
        "🧠", "💡", "🌟", "🏆", "❤️", "🍎", "💧", "😴", "🧹", "🛒",
        "☕", "🎮", "📸", "🏊", "🚴", "⚽", "🍳", "🥗", "💊", "📊",
        "💼", "🤝", "📌", "⏰", "🔔", "🎵", "🎨", "🎭", "🎤", "🎸",
        "🌱", "🐾", "✈️", "🏠", "🎾", "🧩", "♟️", "🔬", "🧪", "📚",
        "🚀", "🌈", "🔥", "⭐", "💎", "🎪", "🎬", "🎹", "🥊", "🏋️",
        "🤸", "⛷️", "🏄", "🧗", "🚶", "🛌", "🧑‍💻", "📱", "🖥️", "⌨️",
        "🎧", "📻", "📺", "🔑", "🏡", "🌍", "🌙", "☀️", "🌊", "🍃",
        "🌺", "🌻", "🍕", "🍔", "🥤", "🍰", "🍩", "🥑", "🫖", "🧃",
        "💰", "📈", "🗂️", "📎", "🔒", "💬", "✅", "❌", "🎁", "🧸",
        "🐶", "🐱", "🦋", "🐝", "🌵", "🍀", "🎂", "🎉", "🎊", "🪴",
        "🏅", "🥇", "👑", "💫", "⚡", "🔮", "🧲", "🎲", "🃏", "🧶"
    ];

    public TaskEditorViewModel(
        IDailyActivityApiService? activityApi,
        INavigationService navigationService,
        IToastService? toastService = null,
        ISubscriptionApiService? subscriptionApiService = null,
        IConfirmDialogService? confirmDialogService = null)
    {
        _activityApi = activityApi;
        _navigationService = navigationService;
        _toastService = toastService;
        _subscriptionApiService = subscriptionApiService;
        _confirmDialogService = confirmDialogService;
        _ = LoadCustomizationPermissionsAsync();
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

    public void ConfigureForCreate(Guid groupId, string groupColor = "#7E57C2")
    {
        _onComplete = null;
        _editingTaskId = null;
        _parentGroupId = groupId;
        IsEditMode = false;
        PageTitle = "New Task";
        TaskTitle = string.Empty;
        TaskDescription = null;
        SelectedTaskType = ActivityItemType.Timer;
        TaskDurationMinutes = 30;
        TaskTargetCount = 8;
        TaskIcon = "✨";
        TaskColor = groupColor;
        IsEmojiPickerOpen = false;
        SelectedVerificationTemplate = VerificationTemplate.Workout;
        CustomVerificationCriteria = null;
    }

    public void ConfigureForEdit(ActivityTaskItemViewModel task, Action? onComplete = null)
    {
        _onComplete = onComplete;
        _editingTaskId = task.Id;
        IsEditMode = true;
        PageTitle = "Edit Task";
        TaskTitle = task.Title;
        TaskDescription = task.Description;
        SelectedTaskType = task.TaskType;
        TaskDurationMinutes = task.DurationMinutes ?? 30;
        TaskTargetCount = task.TargetCount ?? 8;
        TaskIcon = task.Icon;
        TaskColor = task.Color;
        IsEmojiPickerOpen = false;
        SelectedVerificationTemplate = task.VerificationTemplate ?? VerificationTemplate.Workout;
        CustomVerificationCriteria = task.CustomVerificationCriteria;
    }

    [RelayCommand]
    private void SelectTaskType(string type)
    {
        SelectedTaskType = type switch
        {
            "Count" => ActivityItemType.Count,
            "Steps" => ActivityItemType.Steps,
            "Checkbox" => ActivityItemType.Checkbox,
            "PhotoVerification" => ActivityItemType.PhotoVerification,
            _ => ActivityItemType.Timer
        };
    }

    [RelayCommand]
    private void SelectVerificationTemplate(string template)
    {
        if (Enum.TryParse<VerificationTemplate>(template, out var parsed))
            SelectedVerificationTemplate = parsed;
    }

    [RelayCommand]
    private void ToggleEmojiPicker() => IsEmojiPickerOpen = !IsEmojiPickerOpen;

    [RelayCommand]
    private async Task SelectEmoji(string emoji)
    {
        if (!CanCustomizeEmoji)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        TaskIcon = emoji;
        IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    private async Task SelectColor(string color)
    {
        if (!CanCustomizeColors)
        {
            await PromptPremiumUpgradeAsync();
            return;
        }
        TaskColor = color;
    }

    [RelayCommand]
    private void IncrementDuration() => TaskDurationMinutes = Math.Min(TaskDurationMinutes + 5, 480);

    [RelayCommand]
    private void DecrementDuration() => TaskDurationMinutes = Math.Max(TaskDurationMinutes - 5, 5);

    [RelayCommand]
    private void IncrementTargetCount() => TaskTargetCount = Math.Min(TaskTargetCount + 1, 999);

    [RelayCommand]
    private void DecrementTargetCount() => TaskTargetCount = Math.Max(TaskTargetCount - 1, 1);

    [RelayCommand]
    private void IncrementTargetSteps() => TaskTargetCount = Math.Min(TaskTargetCount + 1000, 100000);

    [RelayCommand]
    private void DecrementTargetSteps() => TaskTargetCount = Math.Max(TaskTargetCount - 1000, 1000);

    partial void OnTaskDurationMinutesChanged(int value)
    {
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(PreviewSubtitle));
    }

    partial void OnTaskTargetCountChanged(int value) => OnPropertyChanged(nameof(PreviewSubtitle));

    partial void OnSelectedTaskTypeChanged(ActivityItemType value)
    {
        OnPropertyChanged(nameof(IsTimerType));
        OnPropertyChanged(nameof(IsCountType));
        OnPropertyChanged(nameof(IsStepsType));
        OnPropertyChanged(nameof(IsCheckboxType));
        OnPropertyChanged(nameof(IsPhotoVerificationType));
        OnPropertyChanged(nameof(IsCustomVerification));
        OnPropertyChanged(nameof(PreviewSubtitle));

        // Set sensible defaults when switching types
        if (value == ActivityItemType.Steps && TaskTargetCount < 1000)
            TaskTargetCount = 10000;
        else if (value == ActivityItemType.Count && TaskTargetCount > 100)
            TaskTargetCount = 8;
    }

    [RelayCommand]
    private async Task SaveTask()
    {
        if (string.IsNullOrWhiteSpace(TaskTitle) || _activityApi is null) return;

        IsSaving = true;
        try
        {
            if (IsEditMode && _editingTaskId.HasValue)
            {
                var request = new UpdateActivityItemRequest
                {
                    Title = TaskTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(TaskDescription) ? null : TaskDescription.Trim(),
                    TaskType = SelectedTaskType,
                    DurationMinutes = IsTimerType ? TaskDurationMinutes : null,
                    TargetCount = (IsCountType || IsStepsType) ? TaskTargetCount : null,
                    Icon = TaskIcon,
                    Color = TaskColor,
                    VerificationTemplate = IsPhotoVerificationType ? SelectedVerificationTemplate : null,
                    CustomVerificationCriteria = IsCustomVerification ? CustomVerificationCriteria?.Trim() : null
                };

                await _activityApi.UpdateItemAsync(_editingTaskId.Value, request);
                _toastService?.ShowSuccess("Task updated!");
            }
            else
            {
                var request = new CreateActivityItemRequest
                {
                    ActivityGroupId = _parentGroupId,
                    Title = TaskTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(TaskDescription) ? null : TaskDescription.Trim(),
                    TaskType = SelectedTaskType,
                    DurationMinutes = IsTimerType ? TaskDurationMinutes : null,
                    TargetCount = (IsCountType || IsStepsType) ? TaskTargetCount : null,
                    Icon = TaskIcon,
                    Color = TaskColor,
                    VerificationTemplate = IsPhotoVerificationType ? SelectedVerificationTemplate : null,
                    CustomVerificationCriteria = IsCustomVerification ? CustomVerificationCriteria?.Trim() : null
                };

                await _activityApi.CreateItemAsync(request);
                _toastService?.ShowSuccess("Task created!");
            }

            if (_onComplete is not null)
                _onComplete();
            else
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
        if (_onComplete is not null)
            _onComplete();
        else
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
