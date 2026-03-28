using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class ConfirmDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _confirmText = "Confirm";

    [ObservableProperty]
    private string _cancelText = "Cancel";

    public Action<bool>? CloseRequested { get; set; }

    public void Configure(string title, string message, string confirmText, string cancelText)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }

    [RelayCommand]
    private void Confirm()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
