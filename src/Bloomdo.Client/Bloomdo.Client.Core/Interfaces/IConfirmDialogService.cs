namespace Bloomdo.Client.Core.Interfaces;

public interface IConfirmDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Delete", string cancelText = "Cancel");
}
