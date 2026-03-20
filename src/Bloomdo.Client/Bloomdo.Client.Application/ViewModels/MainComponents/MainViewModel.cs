using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class MainViewModel : PageViewModel
{
    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeSelected))]
    [NotifyPropertyChangedFor(nameof(IsBlocksSelected))]
    [NotifyPropertyChangedFor(nameof(IsStatsSelected))]
    [NotifyPropertyChangedFor(nameof(IsProfileSelected))]
    private int _selectedTabIndex = 0;

    public bool IsHomeSelected => SelectedTabIndex == 0;
    public bool IsBlocksSelected => SelectedTabIndex == 1;
    public bool IsStatsSelected => SelectedTabIndex == 2;
    public bool IsProfileSelected => SelectedTabIndex == 3;

    public ObservableCollection<TabItemViewModel> Tabs { get; }

    public MainViewModel(
        HomeViewModel homeViewModel,
        BlocksViewModel blocksViewModel,
        StatsViewModel statsViewModel,
        ProfileViewModel profileViewModel)
    {
        Tabs = new ObservableCollection<TabItemViewModel>
        {
            new("Home", homeViewModel),
            new("Blocks", blocksViewModel),
            new("Stats", statsViewModel),
            new("Profile", profileViewModel)
        };

        _currentPage = homeViewModel;
        _currentPage.OnAppearing();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value >= 0 && value < Tabs.Count)
        {
            var oldPage = CurrentPage;
            oldPage?.OnDisappearing();

            CurrentPage = Tabs[value].Content;
            CurrentPage?.OnAppearing();
        }
    }

    [RelayCommand]
    private void SelectTab(string? indexStr)
    {
        if (int.TryParse(indexStr, out var index))
            SelectedTabIndex = index;
    }
}
