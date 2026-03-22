using System;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
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
            services.AddSingleton<IAppIconProvider>(sp => new AndroidAppIconProvider(global::Android.App.Application.Context));
            services.AddSingleton<IBlockEnforcementService>(sp => new AndroidBlockEnforcementService(global::Android.App.Application.Context));
        };

        base.OnCreate(savedInstanceState);

        Platform.Init(this, savedInstanceState);

        EnsureRequiredPermissions();
        StartBlockEnforcement();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
    }

    private void EnsureRequiredPermissions()
    {
        // Overlay permission — required on Android 6+ for the foreground service
        // to show BlockedActivity on top of blocked apps
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
            !global::Android.Provider.Settings.CanDrawOverlays(this))
        {
            var intent = new Intent(
                global::Android.Provider.Settings.ActionManageOverlayPermission,
                global::Android.Net.Uri.Parse("package:" + PackageName));
            StartActivity(intent);
        }

        // Usage access permission — required for UsageStatsManager to detect
        // which app is currently in the foreground
        try
        {
            var appOps = (AppOpsManager)GetSystemService(AppOpsService)!;
            var mode = appOps.UnsafeCheckOpNoThrow("android:get_usage_stats",
                Process.MyUid(), PackageName ?? "");

            if (mode != AppOpsManagerMode.Allowed)
            {
                var intent = new Intent(
                    global::Android.Provider.Settings.ActionUsageAccessSettings);
                StartActivity(intent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Usage access check failed: {ex.Message}");
        }
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
