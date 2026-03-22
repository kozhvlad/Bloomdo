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

    private Guid? _editingTaskId;
    private Guid _parentGroupId;
    private Action? _onComplete;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _pageTitle = "New Task";

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

    public bool IsTimerType => SelectedTaskType == ActivityItemType.Timer;
    public bool IsCountType => SelectedTaskType == ActivityItemType.Count;
    public bool IsStepsType => SelectedTaskType == ActivityItemType.Steps;
    public bool IsCheckboxType => SelectedTaskType == ActivityItemType.Checkbox;

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
        IToastService? toastService = null)
    {
        _activityApi = activityApi;
        _navigationService = navigationService;
        _toastService = toastService;
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
    }

    [RelayCommand]
    private void SelectTaskType(string type)
    {
        SelectedTaskType = type switch
        {
            "Count" => ActivityItemType.Count,
            "Steps" => ActivityItemType.Steps,
            "Checkbox" => ActivityItemType.Checkbox,
            _ => ActivityItemType.Timer
        };
    }

    [RelayCommand]
    private void ToggleEmojiPicker() => IsEmojiPickerOpen = !IsEmojiPickerOpen;

    [RelayCommand]
    private void SelectEmoji(string emoji)
    {
        TaskIcon = emoji;
        IsEmojiPickerOpen = false;
    }

    [RelayCommand]
    private void SelectColor(string color) => TaskColor = color;

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
                    Color = TaskColor
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
                    Color = TaskColor
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
}
