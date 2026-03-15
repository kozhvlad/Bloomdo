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
    private int? _durationMinutes;

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
    private int _timerRemainingSeconds;

    public string FirstLetter =>
        string.IsNullOrEmpty(Title) ? "?" : Title[..1].ToUpperInvariant();

    public string DurationLabel =>
        DurationMinutes.HasValue ? $"{DurationMinutes} min" : string.Empty;

    public bool HasDuration => DurationMinutes.HasValue;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => CurrentStreak > 0 ? $"x{CurrentStreak}" : string.Empty;

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
            if (DurationMinutes.HasValue)
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

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(FirstLetter));
    partial void OnTimerRemainingSecondsChanged(int value) => OnPropertyChanged(nameof(TimerDisplayText));
    partial void OnDurationMinutesChanged(int? value)
    {
        OnPropertyChanged(nameof(HasDuration));
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(Subtitle));
    }
}
