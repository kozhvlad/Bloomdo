using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class TimerDialogService : ITimerDialogService
{
    private readonly ShellViewModel _shell;
    private readonly ITimerStateStore _timerStateStore;

    public TimerDialogService(ShellViewModel shell, ITimerStateStore timerStateStore)
    {
        _shell = shell;
        _timerStateStore = timerStateStore;
    }

    public void ShowTimer(Guid taskId, string title, string icon, string color, int durationMinutes, int streak = 0, Action? onClose = null)
    {
        var timerVm = new TimerDialogViewModel(_timerStateStore);

        // Task completion — ONLY when timer reaches zero
        timerVm.TimerCompleted += () =>
        {
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
            onClose?.Invoke();
        };

        // X-button close — just close the overlay, do NOT complete the task
        timerVm.CloseRequested += () =>
        {
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
        };

        // Background tap — save state, do NOT complete the task
        // Note: ShellViewModel.CloseOverlay() already sets OverlayContent = null
        _shell.OnOverlayClosed = () =>
        {
            timerVm.SaveOnDismiss();
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
    }
}
