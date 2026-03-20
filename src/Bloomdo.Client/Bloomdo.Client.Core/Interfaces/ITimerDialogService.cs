namespace Bloomdo.Client.Core.Interfaces;

public interface ITimerDialogService
{
    void ShowTimer(string title, string icon, string color, int durationMinutes, int streak = 0, Action? onClose = null);
}
