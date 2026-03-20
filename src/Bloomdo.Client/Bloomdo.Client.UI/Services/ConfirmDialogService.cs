using Bloomdo.Client.Core.Interfaces;
using ShadUI;

namespace Bloomdo.Client.UI.Services;

public class ConfirmDialogService(DialogManager dialogManager) : IConfirmDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "Delete", string cancelText = "Cancel")
    {
        var tcs = new TaskCompletionSource<bool>();

        dialogManager
            .CreateDialog(title, message)
            .WithPrimaryButton(confirmText, () => tcs.TrySetResult(true), DialogButtonStyle.Destructive)
            .WithCancelButton(cancelText, () => tcs.TrySetResult(false))
            .Dismissible()
            .Show();

        return tcs.Task;
    }
}
