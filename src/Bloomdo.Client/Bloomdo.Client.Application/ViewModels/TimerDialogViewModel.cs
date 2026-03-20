using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class TimerDialogViewModel : ObservableObject
{
    public event Action? CloseRequested;

    [ObservableProperty]
    private string _taskTitle = string.Empty;

    [ObservableProperty]
    private string _taskIcon = "✨";

    [ObservableProperty]
    private string _taskColor = "#FF9800";

    [ObservableProperty]
    private int _totalSeconds;

    [ObservableProperty]
    private int _remainingSeconds;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private int _currentStreak;

    private int _originalDurationMinutes;
    private CancellationTokenSource? _timerCts;

    public string TimerDisplay
    {
        get
        {
            var m = RemainingSeconds / 60;
            var s = RemainingSeconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }

    public double ProgressFraction =>
        TotalSeconds > 0 ? (double)(TotalSeconds - RemainingSeconds) / TotalSeconds : 0;

    public string ProgressPercent =>
        $"{(int)(ProgressFraction * 100)}%";

    public string SubtitleText =>
        $"Every day, {_originalDurationMinutes} minutes";

    public string StatusText => IsRunning
        ? (IsPaused ? "⏸ Paused" : "▶ Running")
        : (RemainingSeconds <= 0 ? "✅ Complete!" : "Ready to start");

    public bool HasStarted => IsRunning || IsPaused;
    public bool HasNotStarted => !IsRunning && RemainingSeconds > 0;
    public bool IsComplete => !IsRunning && RemainingSeconds <= 0;
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => $"🔥{CurrentStreak}";

    public string StartButtonText => IsRunning
        ? (IsPaused ? "▶ Resume" : "⏸ Pause")
        : (RemainingSeconds <= 0 ? "↻ Restart" : "▶ Start timer");

    public void Configure(string title, string icon, string color, int durationMinutes, int streak = 0)
    {
        TaskTitle = title;
        TaskIcon = icon;
        TaskColor = color;
        _originalDurationMinutes = durationMinutes;
        TotalSeconds = durationMinutes * 60;
        RemainingSeconds = TotalSeconds;
        CurrentStreak = streak;
        IsRunning = false;
        IsPaused = false;
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (RemainingSeconds <= 0)
        {
            RemainingSeconds = TotalSeconds;
            IsRunning = true;
            IsPaused = false;
            _timerCts = new CancellationTokenSource();
            _ = RunAsync(_timerCts.Token);
            RefreshComputedProperties();
            return;
        }

        if (IsRunning && !IsPaused)
        {
            IsPaused = true;
            RefreshComputedProperties();
            return;
        }

        if (IsPaused)
        {
            IsPaused = false;
            RefreshComputedProperties();
            return;
        }

        IsRunning = true;
        IsPaused = false;
        _timerCts = new CancellationTokenSource();
        _ = RunAsync(_timerCts.Token);
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void Stop()
    {
        _timerCts?.Cancel();
        _timerCts = null;
        IsRunning = false;
        IsPaused = false;
        RemainingSeconds = TotalSeconds;
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _timerCts?.Cancel();
        _timerCts = null;
        IsRunning = false;
        IsPaused = false;
        RemainingSeconds = TotalSeconds;
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void AddTime(int minutes)
    {
        RemainingSeconds = Math.Max(0, RemainingSeconds + minutes * 60);
        TotalSeconds = Math.Max(TotalSeconds, RemainingSeconds);
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void AdjustTime(int seconds)
    {
        if (IsRunning) return;
        RemainingSeconds = Math.Max(60, RemainingSeconds + seconds);
        TotalSeconds = RemainingSeconds;
        RefreshComputedProperties();
    }

    [RelayCommand]
    private void Close()
    {
        _timerCts?.Cancel();
        _timerCts = null;
        IsRunning = false;
        IsPaused = false;
        CloseRequested?.Invoke();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (RemainingSeconds > 0 && !ct.IsCancellationRequested)
            {
                if (!IsPaused)
                {
                    await Task.Delay(1000, ct);
                    if (!IsPaused)
                    {
                        RemainingSeconds--;
                        RefreshComputedProperties();
                    }
                }
                else
                {
                    await Task.Delay(200, ct);
                }
            }

            if (!ct.IsCancellationRequested)
            {
                IsRunning = false;
                RefreshComputedProperties();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(TimerDisplay));
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SubtitleText));
        OnPropertyChanged(nameof(HasStarted));
        OnPropertyChanged(nameof(HasNotStarted));
        OnPropertyChanged(nameof(IsComplete));
        OnPropertyChanged(nameof(HasStreak));
        OnPropertyChanged(nameof(StreakText));
        OnPropertyChanged(nameof(StartButtonText));
    }

    partial void OnRemainingSecondsChanged(int value) => RefreshComputedProperties();
}
