using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class NoConnectionViewModel : PageViewModel
{
    private readonly ShellViewModel _shellViewModel;

    [ObservableProperty]
    private string _message = "Unable to connect to the server. Please check your internet connection and try again.";

    [ObservableProperty]
    private bool _isRetrying;

    public NoConnectionViewModel(ShellViewModel shellViewModel)
    {
        _shellViewModel = shellViewModel;
    }

    public void SetMessage(string message)
    {
        Message = message;
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        IsRetrying = true;

        try
        {
            await _shellViewModel.InitializeAsync();
        }
        finally
        {
            IsRetrying = false;
        }
    }
}
