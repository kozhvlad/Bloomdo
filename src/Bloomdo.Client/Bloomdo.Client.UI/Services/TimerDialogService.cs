using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class TimerDialogService : ITimerDialogService
{
    private readonly ShellViewModel _shell;
    private readonly ITimerStateStore _timerStateStore;
    private TimerDialogViewModel? _currentVm;

    public event Action? TimerStateChanged;

    public TimerDialogService(ShellViewModel shell, ITimerStateStore timerStateStore)
    {
        _shell = shell;
        _timerStateStore = timerStateStore;
    }

    public void ShowTimer(Guid taskId, string title, string icon, string color, int durationMinutes, int streak = 0, Action? onClose = null)
    {
        // Clean up any previous timer VM that may still be running in the
        // background (e.g. user navigated away without closing the overlay).
        // SaveOnDismiss cancels the loop and saves state.
        _currentVm?.SaveOnDismiss();

        var timerVm = new TimerDialogViewModel(_timerStateStore);
        _currentVm = timerVm;

        // Task completion — ONLY when timer reaches zero
        timerVm.TimerCompleted += () =>
        {
            if (_currentVm == timerVm) _currentVm = null;
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
            onClose?.Invoke();
            TimerStateChanged?.Invoke();
        };

        // X-button close — just close the overlay, do NOT complete the task
        timerVm.CloseRequested += () =>
        {
            if (_currentVm == timerVm) _currentVm = null;
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
            TimerStateChanged?.Invoke();
        };

        // Background tap / navigation dismiss — save state, do NOT complete the task.
        // ShellViewModel.CloseOverlay() and ShellViewModel.SetViewModel() both
        // invoke this callback before clearing OverlayContent.
        _shell.OnOverlayClosed = () =>
        {
            timerVm.SaveOnDismiss();
            if (_currentVm == timerVm) _currentVm = null;
            TimerStateChanged?.Invoke();
        };

        // Load saved state first, then show overlay
        _ = InitAndShowAsync(timerVm, taskId, title, icon, color, durationMinutes, streak);
    }

    private async Task InitAndShowAsync(
        TimerDialogViewModel vm, Guid taskId,
        string title, string icon, string color,
        int durationMinutes, int streak)
    {
        await vm.ConfigureAsync(taskId, title, icon, color, durationMinutes, streak);
        _shell.OverlayContent = vm;
        TimerStateChanged?.Invoke();
    }
}
