using Avalonia.Controls;
using ShadUI;

namespace Bloomdo.Client.UI;

public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
    }

    public void SetToastManager(ToastManager manager)
    {
        if (this.FindControl<ToastHost>("ToastHostControl") is { } toastHost)
        {
            toastHost.Manager = manager;
        }
    }

    public void SetDialogManager(DialogManager manager)
    {
        if (this.FindControl<DialogHost>("DialogHostControl") is { } dialogHost)
        {
            dialogHost.Manager = manager;
        }
    }
}