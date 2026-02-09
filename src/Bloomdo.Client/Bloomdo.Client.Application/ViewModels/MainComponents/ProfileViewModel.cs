using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class ProfileViewModel : PageViewModel
{
    [ObservableProperty]
    private string _name = "Alex Johnson";

    [ObservableProperty]
    private string _username = "@alex_j";

    [ObservableProperty]
    private string _initials = "AJ";

    [ObservableProperty]
    private int _streakDays = 12;

    [ObservableProperty]
    private int _tasksCompleted = 145;

    [ObservableProperty]
    private int _focusHours = 32;

    [ObservableProperty]
    private string _level = "Pro Member";

    public ProfileViewModel()
    {
    }
}
