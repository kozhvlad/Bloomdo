using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.UI;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;

namespace Bloomdo.Client.Startup;

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
        var shellViewModel = serviceProvider.GetRequiredService<ShellViewModel>();
        var toastManager = serviceProvider.GetRequiredService<ToastManager>();
        var dialogManager = serviceProvider.GetRequiredService<DialogManager>();

        var themeService = serviceProvider.GetRequiredService<IThemeService>();
        Current!.RequestedThemeVariant = themeService.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var mainWindow = new MainWindow
            {
                DataContext = shellViewModel
            };
            mainWindow.SetToastManager(toastManager);
            mainWindow.SetDialogManager(dialogManager);
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var shellView = new ShellView
            {
                DataContext = shellViewModel
            };
            shellView.SetToastManager(toastManager);
            shellView.SetDialogManager(dialogManager);
            singleViewPlatform.MainView = shellView;
        }

        await shellViewModel.InitializeAsync();
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