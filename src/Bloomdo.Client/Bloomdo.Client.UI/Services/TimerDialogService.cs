using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class TimerDialogService : ITimerDialogService
{
    private readonly ShellViewModel _shell;

    public TimerDialogService(ShellViewModel shell)
    {
        _shell = shell;
    }

    public void ShowTimer(string title, string icon, string color, int durationMinutes, int streak = 0, Action? onClose = null)
    {
        var timerVm = new TimerDialogViewModel();
        timerVm.Configure(title, icon, color, durationMinutes, streak);

        timerVm.CloseRequested += () =>
        {
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
            onClose?.Invoke();
        };

        _shell.OnOverlayClosed = () =>
        {
            // Background tap dismissal — don't invoke onClose (user didn't complete)
        };

        _shell.OverlayContent = timerVm;
    }
}
