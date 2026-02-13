using Android.App;
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
        };

        base.OnCreate(savedInstanceState);

        Platform.Init(this, savedInstanceState);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
    }
}
