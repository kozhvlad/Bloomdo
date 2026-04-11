using System;
using System.IO;
using System.Net.Http;
using Bloomdo.Client.Application.Services;
using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Application.ViewModels.OnbordingComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Infrastructure.DatabaseContexts;
using Bloomdo.Client.Infrastructure.Middleware;
using Bloomdo.Client.Infrastructure.Services;
using Bloomdo.Client.UI.Services;
using Bloomdo.Client.UI.Dialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;

namespace Bloomdo.Client.Startup;

public static class DependencyContainer
{
    private static IServiceProvider? _serviceProvider;
    public static Action<IServiceCollection>? RegisterPlatformServices { get; set; }

	public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider has not been configured.");
            }

            return _serviceProvider;
        }
    }

    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        RegisterServices(services);
        RegisterRepositories(services);
        RegisterHttpClients(services);
        RegisterViewModels(services);

        RegisterPlatformServices?.Invoke(services);

        var dbPath = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "BloomdoLocal.db");
        services.AddDbContext<LocalDatabaseContext>(options => options.UseSqlite($"Filename={dbPath}"));

        _serviceProvider = services.BuildServiceProvider();

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LocalDatabaseContext>();
            dbContext.Database.Migrate();
        }

        return _serviceProvider;
    }
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ITokenStorage, TokenStorage>();

        // Func<> factories for lazy resolution (breaks circular dependencies)
        services.AddSingleton<Func<IAuthApiService>>(sp => () => sp.GetRequiredService<IAuthApiService>());
        services.AddSingleton<Func<INavigationService>>(sp => () => sp.GetRequiredService<INavigationService>());

        services.AddSingleton<IAccessTokenManager, AccessTokenManager>();

        // Preferences
        services.AddSingleton<IPreferencesService, PreferencesService>();

        // Authorization service
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Toast system
        services.AddSingleton<ToastManager>();
        services.AddSingleton<IToastService, ToastService>();

        // Dialog system
        services.AddSingleton<DialogManager>();
        services.AddSingleton<ITimerDialogService, TimerDialogService>();
        services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();

        // Theme service
        services.AddSingleton<IThemeService, ThemeService>();

        // NavigationService
        services.AddSingleton<INavigationService, NavigationService>();

        // Block rule local store
        services.AddSingleton<IBlockRuleStore, BlockRuleStore>();

        // Timer state persistence
        services.AddSingleton<ITimerStateStore, LocalTimerStateStore>();

        // Group completion local store
        services.AddSingleton<IGroupCompletionStore, GroupCompletionStore>();

        // Browser service
        services.AddSingleton<IBrowserService, BrowserService>();

        // Local usage cache
        services.AddSingleton<ILocalUsageStore, LocalUsageStore>();

        // Local profile cache (offline profile data)
        services.AddSingleton<ILocalProfileStore, LocalProfileStore>();

        // Local stats cache (offline stats: calendar, weekly, daily)
        services.AddSingleton<ILocalStatsStore, LocalStatsStore>();

        // Local subscription cache (offline premium status)
        services.AddSingleton<ILocalSubscriptionStore, LocalSubscriptionStore>();

        // Usage sync service (local cache + server sync)
        services.AddSingleton<IUsageSyncService, UsageSyncService>();

        // Connectivity service
        services.AddSingleton<IConnectivityService, ConnectivityService>();

        // Local activity cache (offline queued toggles)
        services.AddSingleton<ILocalActivityCache, LocalActivityCache>();

        // Image picker service
        services.AddSingleton<IImagePickerService, AvaloniaImagePickerService>();

        // Photo verification dialog service
        services.AddSingleton<IPhotoVerificationDialogService, PhotoVerificationDialogService>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {

    }

    private static void RegisterHttpClients(IServiceCollection services)
    {
        services.AddTransient<AuthHeaderHandler>();

        var apiBaseUrl = "https://10.0.2.2:7270/";

        // SignalR client (singleton, connects after auth)
        services.AddSingleton<ISignalRClientService>(new SignalRClientService(apiBaseUrl));

        // Auth endpoints (login, register, refresh) don't require authentication
        // so we don't add AuthHeaderHandler here to avoid circular dependency
        services.AddHttpClient<IAuthApiService, AuthApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => 
        {
            var handler = new HttpClientHandler();
#if DEBUG
            // For development with self-signed certificates on Android emulator
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        // For other API services that need authentication, add AuthHeaderHandler:
        services.AddHttpClient<IStatsApiService, StatsApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<IBlockApiService, BlockApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<IDailyActivityApiService, DailyActivityApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<IProfileApiService, ProfileApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<IChatApiService, ChatApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(120);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<ISubscriptionApiService, SubscriptionApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<IFriendsApiService, FriendsApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });

        services.AddHttpClient<ISocialApiService, SocialApiService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            return handler;
        });
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();

        // Auth views
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<AccessDeniedViewModel>();
        services.AddTransient<NoConnectionViewModel>();

        // Onboarding components
        services.AddSingleton<OnboardingViewModel>();
        services.AddTransient<WelcomeStepViewModel>();
        services.AddTransient<AskNameStepViewModel>();
        services.AddTransient<SetGoalsStepViewModel>();

        // Main view and tabs
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<BlocksViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<AiChatViewModel>();
        services.AddTransient<SubscriptionViewModel>();
        services.AddTransient<ProfileViewModel>();

        // Social
        services.AddTransient<SocialViewModel>();
        services.AddTransient<UserSearchViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<FollowListViewModel>();
        services.AddTransient<UserProfileViewModel>();
        services.AddTransient<SharedGroupDetailViewModel>();
        services.AddTransient<SharedGroupEditorViewModel>();

        // Profile editors
        services.AddTransient<AvatarEditorViewModel>();
        services.AddTransient<ProfileEditorViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AccountEditorViewModel>();

        // Activity editors (singleton — preserves state across navigation)
        services.AddSingleton<GroupEditorViewModel>();
        services.AddSingleton<TaskEditorViewModel>();
    }
}