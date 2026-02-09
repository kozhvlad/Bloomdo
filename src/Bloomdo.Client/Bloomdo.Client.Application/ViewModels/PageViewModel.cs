using Bloomdo.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Application.ViewModels;

public class PageViewModel : ObservableObject, IPage
{
    public virtual void OnAppearing() { }
    public virtual void OnDisappearing() { }
}