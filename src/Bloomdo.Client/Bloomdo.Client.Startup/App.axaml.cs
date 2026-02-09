using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Bloomdo.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Bloomdo.UI;
using Avalonia.Styling;
using Bloomdo.Application.ViewModels;
using Bloomdo.Application.ViewModels.MainComponents;
using Bloomdo.Application.ViewModels.OnbordingComponents;

namespace Bloomdo.Startup;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        var serviceProvider = DependencyContainer.ConfigureServices();
        var authService = serviceProvider.GetRequiredService<IAccessTokenManager>();
        var navigationService = serviceProvider.GetRequiredService<INavigationService>();
        var shellViewModel = serviceProvider.GetRequiredService<ShellViewModel>();

        Current!.RequestedThemeVariant = ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = new MainWindow
            {
                DataContext = shellViewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new ShellView()
            {
                DataContext = shellViewModel
            };
        }

        await authService.TryLoadTokenFromStorage();

        if (authService.IsAuthenticated)
        {
            // navigationService.NavigateTo<DashboardViewModel>(); 
        }
        else
        {
            // navigationService.NavigateTo<OnboardingViewModel>();
        }

        navigationService.NavigateTo<OnboardingViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}