using Bloomdo.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace Bloomdo.Application.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private IPage _currentViewModel = null!;

    public void SetViewModel(IPage viewModel)
    {
        Debug.WriteLine($"ShellViewModel.SetViewModel called with {viewModel?.GetType().Name ?? "null"}");
        CurrentViewModel = viewModel;
        Debug.WriteLine($"CurrentViewModel is now {CurrentViewModel?.GetType().Name ?? "null"}");
    }
}