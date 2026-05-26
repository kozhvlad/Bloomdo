using Bloomdo.Shared.DTOs.Activities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class ActivityTaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private ActivityItemType _taskType;

    [ObservableProperty]
    private int? _durationMinutes;

    [ObservableProperty]
    private int? _targetCount;

    [ObservableProperty]
    private int _currentCount;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _color = "#7E57C2";

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private DateTime? _completedAtUtc;

    [ObservableProperty]
    private bool _isToggling;

    // Editing
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string? _editDescription;

    [ObservableProperty]
    private int? _editDurationMinutes;

    [ObservableProperty]
    private string _editColor = "#7E57C2";

    // Timer
    [ObservableProperty]
    private bool _isTimerRunning;

    [ObservableProperty]
    private bool _isTimerPaused;

    [ObservableProperty]
    private int _timerRemainingSeconds;

    public bool HasActiveTimer => IsTimerRunning || IsTimerPaused;

    public bool ShowIdleTimerButton => HasDuration && !HasActiveTimer;

    public string TimerStatusIcon =>
        IsTimerPaused ? "⏸" : (IsTimerRunning ? "▶" : string.Empty);

    [ObservableProperty]
    private VerificationTemplate? _verificationTemplate;

    [ObservableProperty]
    private string? _customVerificationCriteria;

    // Computed — type checks
    public bool IsTimerType => TaskType == ActivityItemType.Timer;
    public bool IsCountType => TaskType == ActivityItemType.Count;
    public bool IsStepsType => TaskType == ActivityItemType.Steps;
    public bool IsCheckboxType => TaskType == ActivityItemType.Checkbox;
    public bool IsPhotoVerificationType => TaskType == ActivityItemType.PhotoVerification;
    public bool IsCountBasedType => IsCountType || IsStepsType;

    public string FirstLetter =>
        string.IsNullOrEmpty(Title) ? "?" : Title[..1].ToUpperInvariant();

    public string DurationLabel =>
        DurationMinutes.HasValue ? $"{DurationMinutes} min" : string.Empty;

    public bool HasDuration => IsTimerType && DurationMinutes.HasValue;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => CurrentStreak > 0 ? $"x{CurrentStreak}" : string.Empty;

    // Count-type computed
    public string CountProgressText => $"{CurrentCount}/{TargetCount ?? 0}";
    public bool IsCountComplete => IsCountBasedType && TargetCount.HasValue && CurrentCount >= TargetCount.Value;
    public double CountFraction => IsCountBasedType && TargetCount is > 0
        ? Math.Min(1.0, (double)CurrentCount / TargetCount.Value)
        : 0;

    // Steps-type computed
    public string StepsProgressText => $"{CurrentCount}/{TargetCount ?? 0}";

    public string TimerDisplayText
    {
        get
        {
            var m = TimerRemainingSeconds / 60;
            var s = TimerRemainingSeconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }

    public string Subtitle
    {
        get
        {
            var parts = new List<string>();
            if (IsCountType && TargetCount.HasValue)
                parts.Add($"{CurrentCount}/{TargetCount} done");
            else if (IsStepsType && TargetCount.HasValue)
                parts.Add($"{CurrentCount}/{TargetCount} steps");
            else if (IsCheckboxType)
                parts.Add(IsCompleted ? "Done" : "Tap to complete");
            else if (IsPhotoVerificationType)
                parts.Add(IsCompleted ? "Verified" : "Tap to verify with photo");
            else if (DurationMinutes.HasValue)
                parts.Add($"{DurationMinutes} min");
            if (!string.IsNullOrWhiteSpace(Description))
                parts.Add(Description);
            return parts.Count > 0 ? string.Join(" · ", parts) : "Every day";
        }
    }

    public void StartEdit()
    {
        EditTitle = Title;
        EditDescription = Description;
        EditDurationMinutes = DurationMinutes;
        EditColor = Color;
        IsEditing = true;
    }

    public void RefreshCountProperties()
    {
        OnPropertyChanged(nameof(CountProgressText));
        OnPropertyChanged(nameof(StepsProgressText));
        OnPropertyChanged(nameof(IsCountComplete));
        OnPropertyChanged(nameof(CountFraction));
        OnPropertyChanged(nameof(Subtitle));
    }

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(FirstLetter));
    partial void OnTimerRemainingSecondsChanged(int value) => OnPropertyChanged(nameof(TimerDisplayText));
    partial void OnIsTimerRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(HasActiveTimer));
        OnPropertyChanged(nameof(ShowIdleTimerButton));
        OnPropertyChanged(nameof(TimerStatusIcon));
    }
    partial void OnIsTimerPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(HasActiveTimer));
        OnPropertyChanged(nameof(ShowIdleTimerButton));
        OnPropertyChanged(nameof(TimerStatusIcon));
    }
    partial void OnTaskTypeChanged(ActivityItemType value)
    {
        OnPropertyChanged(nameof(IsTimerType));
        OnPropertyChanged(nameof(IsCountType));
        OnPropertyChanged(nameof(IsStepsType));
        OnPropertyChanged(nameof(IsCheckboxType));
        OnPropertyChanged(nameof(IsPhotoVerificationType));
        OnPropertyChanged(nameof(IsCountBasedType));
        OnPropertyChanged(nameof(HasDuration));
        OnPropertyChanged(nameof(ShowIdleTimerButton));
        OnPropertyChanged(nameof(Subtitle));
    }
    partial void OnDurationMinutesChanged(int? value)
    {
        OnPropertyChanged(nameof(HasDuration));
        OnPropertyChanged(nameof(ShowIdleTimerButton));
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(Subtitle));
    }
    partial void OnTargetCountChanged(int? value) => RefreshCountProperties();
    partial void OnCurrentCountChanged(int value) => RefreshCountProperties();
}
