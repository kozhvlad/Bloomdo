using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Application.ViewModels.MainComponents;

public partial class HomeViewModel : PageViewModel
{
    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Bloomdo!";

    public HomeViewModel()
    {
    }
}
