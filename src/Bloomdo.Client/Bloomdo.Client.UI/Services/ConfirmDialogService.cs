using System.Threading.Tasks;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class ConfirmDialogService : IConfirmDialogService
{
    private readonly ShellViewModel _shell;

    public ConfirmDialogService(ShellViewModel shell)
    {
        _shell = shell;
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "Delete", string cancelText = "Cancel")
    {
        var tcs = new TaskCompletionSource<bool>();

        var dialogVm = new ConfirmDialogViewModel();
        dialogVm.Configure(title, message, confirmText, cancelText);

        dialogVm.CloseRequested = (result) =>
        {
            _shell.OnOverlayClosed = null;
            _shell.OverlayContent = null;
            tcs.TrySetResult(result);
        };

        _shell.OnOverlayClosed = () =>
        {
            // Background tap dismissal
            tcs.TrySetResult(false);
        };

        _shell.OverlayContent = dialogVm;

        return tcs.Task;
    }
}
