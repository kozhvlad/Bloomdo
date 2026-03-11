using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Bloomdo.Client.Android.Services;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Bloomdo.Client.Android;

[Activity(
    Label = "Bloomdo.Client.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        DependencyContainer.RegisterPlatformServices = services =>
        {
            services.AddSingleton<IAppUsageService>(sp => new AndroidAppUsageService(global::Android.App.Application.Context));
            services.AddSingleton<IInstalledAppsService>(sp => new AndroidInstalledAppsService(global::Android.App.Application.Context));
            services.AddSingleton<IBlockEnforcementService>(sp => new AndroidBlockEnforcementService(global::Android.App.Application.Context));
        };

        base.OnCreate(savedInstanceState);

        Platform.Init(this, savedInstanceState);

        StartBlockEnforcement();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
    }

    private static void StartBlockEnforcement()
    {
        try
        {
            var enforcement = DependencyContainer.ServiceProvider.GetService<IBlockEnforcementService>();
            enforcement?.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start enforcement: {ex.Message}");
        }
    }
}
