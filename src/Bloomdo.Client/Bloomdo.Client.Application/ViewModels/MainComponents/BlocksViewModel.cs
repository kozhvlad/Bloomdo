using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class BlocksViewModel : PageViewModel
{
    [ObservableProperty]
    private bool _isMenuOpen;

    public ObservableCollection<BlockerItem> Blockers { get; } = new();

    public BlocksViewModel()
    {
        Blockers.Add(new BlockerItem("Morning Focus", "08:35 - 11:35", true, true));
        Blockers.Add(new BlockerItem("Social", "1h 30m daily", false, true));
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }
}