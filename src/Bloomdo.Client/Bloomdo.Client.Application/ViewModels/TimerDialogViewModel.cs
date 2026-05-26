using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class TimerDialogViewModel : ObservableObject
{
    /// <summary>Fired when user explicitly closes the dialog (X button).</summary>
    public event Action? CloseRequested;

    /// <summary>Fired ONLY when the timer naturally reaches zero.</summary>
    public event Action? TimerCompleted;

    private readonly ITimerStateStore? _stateStore;

    [ObservableProperty]
    private Guid _taskId;

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
    private int _ticksSinceLastSave;
    private const int SaveIntervalTicks = 10;

    public TimerDialogViewModel(ITimerStateStore? stateStore = null)
    {
        _stateStore = stateStore;
    }

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
        ? (IsPaused ? "⏸️ Paused" : "▶️ Running")
        : (RemainingSeconds <= 0 ? "✅ Complete!" : "Ready to start");

    public bool HasStarted => IsRunning || IsPaused;
    public bool HasNotStarted => !IsRunning && RemainingSeconds > 0;
    public bool IsComplete => !IsRunning && RemainingSeconds <= 0;
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => $"🔥{CurrentStreak}";

    public string StartButtonIcon => IsRunning
        ? (IsPaused ? "▶️" : "⏸️")
        : (RemainingSeconds <= 0 ? "↻" : "▶️");

    public string StartButtonLabel => IsRunning
        ? (IsPaused ? "Resume" : "Pause")
        : (RemainingSeconds <= 0 ? "Restart" : "Start timer");

    public string StartButtonText => $"{StartButtonIcon} {StartButtonLabel}";

    public async Task ConfigureAsync(Guid taskId, string title, string icon, string color, int durationMinutes, int streak = 0)
    {
        TaskId = taskId;
        _originalDurationMinutes = durationMinutes;

        // Try to restore saved state for this task (today only)
        var saved = _stateStore is not null ? await _stateStore.LoadAsync(taskId) : null;

        if (saved is not null && saved.RemainingSeconds > 0)
        {
            TaskTitle = saved.TaskTitle;
            TaskIcon = saved.TaskIcon;
            TaskColor = saved.TaskColor;
            TotalSeconds = saved.TotalSeconds;
            CurrentStreak = saved.Streak;
            _originalDurationMinutes = saved.DurationMinutes;

            if (saved.IsRunning && !saved.IsPaused)
            {
                // Timer was actively running — account for real elapsed time
                var elapsed = (int)(DateTime.UtcNow - saved.LastTickUtc).TotalSeconds;
                RemainingSeconds = Math.Max(0, saved.RemainingSeconds - elapsed);

                if (RemainingSeconds > 0)
                {
                    IsRunning = true;
                    IsPaused = false;
                    _timerCts = new CancellationTokenSource();
                    _ = RunAsync(_timerCts.Token);
                }
                else
                {
                    // Timer completed while the dialog was closed — fire completion
                    RemainingSeconds = 0;
                    IsRunning = false;
                    IsPaused = false;
                    await ClearStateAsync();
                    TimerCompleted?.Invoke();
                }
            }
            else
            {
                // Timer was paused or not started — restore as-is
                RemainingSeconds = saved.RemainingSeconds;
                IsRunning = saved.IsRunning;
                IsPaused = saved.IsPaused;

                // If the saved state was "running but paused", start the loop so
                // resume works on next PlayPause. The loop checks IsPaused per tick.
                if (IsRunning)
                {
                    _timerCts = new CancellationTokenSource();
                    _ = RunAsync(_timerCts.Token);
                }
            }
        }
        else
        {
            // No saved state — fresh start
            TaskTitle = title;
            TaskIcon = icon;
            TaskColor = color;
            TotalSeconds = durationMinutes * 60;
            RemainingSeconds = TotalSeconds;
            CurrentStreak = streak;
            IsRunning = false;
            IsPaused = false;
        }

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
            _ = SaveStateAsync();
            return;
        }

        if (IsRunning && !IsPaused)
        {
            IsPaused = true;
            RefreshComputedProperties();
            _ = SaveStateAsync();
            return;
        }

        if (IsPaused)
        {
            IsPaused = false;
            // Defensive: if the loop isn't alive (e.g. restored from saved
            // "running + paused" state), start a fresh one. Otherwise resume
            // would visually flip the icon but never tick.
            if (_timerCts is null)
            {
                IsRunning = true;
                _timerCts = new CancellationTokenSource();
                _ = RunAsync(_timerCts.Token);
            }
            RefreshComputedProperties();
            _ = SaveStateAsync();
            return;
        }

        IsRunning = true;
        IsPaused = false;
        _timerCts = new CancellationTokenSource();
        _ = RunAsync(_timerCts.Token);
        RefreshComputedProperties();
        _ = SaveStateAsync();
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
        _ = ClearStateAsync();
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
        _ = ClearStateAsync();
    }

    [RelayCommand]
    private void AddTime(int minutes)
    {
        RemainingSeconds = Math.Max(0, RemainingSeconds + minutes * 60);
        TotalSeconds = Math.Max(TotalSeconds, RemainingSeconds);
        RefreshComputedProperties();
        _ = SaveStateAsync();
    }

    [RelayCommand]
    private void AdjustTime(int seconds)
    {
        if (IsRunning) return;
        RemainingSeconds = Math.Max(60, RemainingSeconds + seconds);
        TotalSeconds = RemainingSeconds;
        RefreshComputedProperties();
        _ = SaveStateAsync();
    }

    [RelayCommand]
    private void Close()
    {
        _timerCts?.Cancel();
        _timerCts = null;

        if (RemainingSeconds <= 0)
        {
            // Timer was complete — signal completion and clear state
            _ = ClearStateAsync();
            IsRunning = false;
            IsPaused = false;
            TimerCompleted?.Invoke();
            CloseRequested?.Invoke();
        }
        else
        {
            // Timer still in progress — save state for later resume
            _ = SaveStateAsync();
            IsRunning = false;
            IsPaused = false;
            CloseRequested?.Invoke();
        }
    }

    /// <summary>
    /// Called on background tap dismissal. Saves state without firing any events.
    /// The overlay is already closed by ShellViewModel.CloseOverlay().
    /// </summary>
    public void SaveOnDismiss()
    {
        _timerCts?.Cancel();
        _timerCts = null;

        // Snapshot capture is synchronous, so the current IsRunning/IsPaused
        // values are recorded correctly. Do NOT mutate those fields here —
        // the VM is being abandoned, and mutating them adds zero value while
        // making the "still running on disk" intent confusing.
        if ((IsRunning || IsPaused) && RemainingSeconds > 0)
        {
            _ = SaveStateAsync();
        }
        else if (RemainingSeconds <= 0)
        {
            _ = ClearStateAsync();
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            _ticksSinceLastSave = 0;

            while (RemainingSeconds > 0 && !ct.IsCancellationRequested)
            {
                if (!IsPaused)
                {
                    await Task.Delay(1000, ct);
                    if (!IsPaused)
                    {
                        RemainingSeconds--;
                        _ticksSinceLastSave++;
                        RefreshComputedProperties();

                        // Periodic save (every N ticks) to avoid excessive I/O
                        if (_ticksSinceLastSave >= SaveIntervalTicks)
                        {
                            _ticksSinceLastSave = 0;
                            _ = SaveStateAsync();
                        }
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
                _ = ClearStateAsync();
                TimerCompleted?.Invoke();
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task SaveStateAsync()
    {
        if (_stateStore is null) return;

        var snapshot = new TimerStateSnapshot
        {
            TaskId = TaskId,
            TaskTitle = TaskTitle,
            TaskIcon = TaskIcon,
            TaskColor = TaskColor,
            TotalSeconds = TotalSeconds,
            RemainingSeconds = RemainingSeconds,
            DurationMinutes = _originalDurationMinutes,
            Streak = CurrentStreak,
            IsRunning = IsRunning,
            IsPaused = IsPaused,
            LastTickUtc = DateTime.UtcNow,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        try
        {
            await _stateStore.SaveAsync(snapshot);
        }
        catch
        {
            // Best-effort persistence — don't crash the timer
        }
    }

    private async Task ClearStateAsync()
    {
        if (_stateStore is null) return;

        try
        {
            await _stateStore.ClearAsync(TaskId);
        }
        catch
        {
            // Best-effort
        }
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
        OnPropertyChanged(nameof(StartButtonIcon));
        OnPropertyChanged(nameof(StartButtonLabel));
    }

    partial void OnRemainingSecondsChanged(int value) => RefreshComputedProperties();
}
