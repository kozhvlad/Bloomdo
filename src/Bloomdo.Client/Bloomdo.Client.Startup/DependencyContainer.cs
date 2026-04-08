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

        services.AddSingleton<IAccessTokenManager>(sp =>
            new AccessTokenManager(() => sp.GetRequiredService<IAuthApiService>(), sp.GetRequiredService<ITokenStorage>()));

        // Preferences
        services.AddSingleton<IPreferencesService, PreferencesService>();

        // Authorization service
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Toast system
        services.AddSingleton<ToastManager>();
        services.AddSingleton<IToastService, ToastService>();

        // Dialog system
        services.AddSingleton(sp =>
        {
            var manager = new DialogManager();
            return manager;
        });
        services.AddSingleton<ITimerDialogService>(sp =>
            new TimerDialogService(sp.GetRequiredService<ShellViewModel>()));
        services.AddSingleton<IConfirmDialogService>(sp => 
            new ConfirmDialogService(sp.GetRequiredService<ShellViewModel>()));

        // Theme service
        services.AddSingleton<IThemeService, ThemeService>();

        // NavigationService
        services.AddSingleton<INavigationService>(sp =>
        {
            var shellViewModel = sp.GetRequiredService<ShellViewModel>();
            var authorizationService = sp.GetRequiredService<IAuthorizationService>();
            var toastService = sp.GetRequiredService<IToastService>();
            return new NavigationService(sp, authorizationService, toastService, shellViewModel);
        });

        // Block rule local store
        services.AddSingleton<IBlockRuleStore, BlockRuleStore>();

        // Group completion local store
        services.AddSingleton<IGroupCompletionStore, GroupCompletionStore>();

        // Browser service
        services.AddSingleton<IBrowserService, BrowserService>();

        // Image picker service
        services.AddSingleton<IImagePickerService, AvaloniaImagePickerService>();

        // Photo verification dialog service
        services.AddSingleton<IPhotoVerificationDialogService>(sp =>
            new PhotoVerificationDialogService(
                sp.GetRequiredService<ShellViewModel>(),
                sp.GetRequiredService<IDailyActivityApiService>(),
                sp.GetRequiredService<IImagePickerService>(),
                sp.GetRequiredService<IToastService>()));
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
        services.AddSingleton<ShellViewModel>(sp =>
        {
            var tokenManager = sp.GetRequiredService<IAccessTokenManager>();
            var preferencesService = sp.GetRequiredService<IPreferencesService>();
            return new ShellViewModel(tokenManager, preferencesService, () => sp.GetRequiredService<INavigationService>());
        });
        
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
        services.AddTransient<MainViewModel>(sp => new MainViewModel(
            sp.GetRequiredService<HomeViewModel>(),
            sp.GetRequiredService<SocialViewModel>(),
            sp.GetRequiredService<BlocksViewModel>(),
            sp.GetRequiredService<StatsViewModel>(),
            sp.GetRequiredService<AiChatViewModel>(),
            sp.GetRequiredService<SubscriptionViewModel>(),
            sp.GetRequiredService<ProfileViewModel>(),
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<ISignalRClientService>()));
        services.AddTransient<HomeViewModel>(sp => new HomeViewModel(
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetRequiredService<IGroupCompletionStore>(),
            sp.GetRequiredService<IBlockRuleStore>(),
            sp.GetRequiredService<IBlockApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<ITimerDialogService>(),
            sp.GetRequiredService<IConfirmDialogService>(),
            sp.GetRequiredService<IPhotoVerificationDialogService>(),
            sp.GetRequiredService<IToastService>()));
        services.AddTransient<BlocksViewModel>(sp => new BlocksViewModel(
            sp.GetRequiredService<IBlockApiService>(),
            sp.GetService<IInstalledAppsService>(),
            sp.GetService<IBlockRuleStore>(),
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetService<IAppIconProvider>(),
            sp.GetRequiredService<ISubscriptionApiService>()));
        services.AddTransient<StatsViewModel>(sp =>
        {
            var appUsageService = sp.GetService<IAppUsageService>();
            var statsApiService = sp.GetRequiredService<IStatsApiService>();
            var appIconProvider = sp.GetService<IAppIconProvider>();
            var subscriptionApiService = sp.GetRequiredService<ISubscriptionApiService>();
            return new StatsViewModel(appUsageService, statsApiService, appIconProvider, subscriptionApiService);
        });
        services.AddTransient<AiChatViewModel>(sp => new AiChatViewModel(
            sp.GetRequiredService<IChatApiService>(),
            sp.GetRequiredService<ISubscriptionApiService>(),
            sp.GetService<IAppUsageService>()));
        services.AddTransient<SubscriptionViewModel>(sp => new SubscriptionViewModel(
            sp.GetRequiredService<ISubscriptionApiService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<IBrowserService>()));
        services.AddTransient<ProfileViewModel>(sp => new ProfileViewModel(
            sp.GetRequiredService<IAccessTokenManager>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IProfileApiService>(),
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<ISubscriptionApiService>()));

        services.AddTransient<SocialViewModel>(sp => new SocialViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<IConfirmDialogService>(),
            sp.GetRequiredService<ISignalRClientService>()));

        services.AddTransient<UserSearchViewModel>(sp => new UserSearchViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>()));

        services.AddTransient<NotificationsViewModel>(sp => new NotificationsViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<INavigationService>()));

        services.AddTransient<FollowListViewModel>(sp => new FollowListViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>()));

        services.AddTransient<UserProfileViewModel>(sp => new UserProfileViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>()));

        services.AddTransient<SharedGroupDetailViewModel>(sp => new SharedGroupDetailViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetRequiredService<ISignalRClientService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<IConfirmDialogService>()));

        services.AddTransient<SharedGroupEditorViewModel>(sp => new SharedGroupEditorViewModel(
            sp.GetRequiredService<ISocialApiService>(),
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<IConfirmDialogService>(),
            sp.GetRequiredService<ISubscriptionApiService>()));

        services.AddTransient<AvatarEditorViewModel>(sp => new AvatarEditorViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IAccessTokenManager>(),
            sp.GetRequiredService<IProfileApiService>()));

        services.AddTransient<ProfileEditorViewModel>(sp => new ProfileEditorViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IAccessTokenManager>(),
            sp.GetRequiredService<IProfileApiService>(),
            sp.GetRequiredService<AvatarEditorViewModel>()));

        services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IAccessTokenManager>(),
            sp.GetRequiredService<IThemeService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<IPreferencesService>(),
            sp.GetRequiredService<IProfileApiService>()));

        services.AddTransient<AccountEditorViewModel>(sp => new AccountEditorViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IAccessTokenManager>(),
            sp.GetRequiredService<IProfileApiService>(),
            sp.GetRequiredService<IToastService>()));

        services.AddSingleton<GroupEditorViewModel>(sp => new GroupEditorViewModel(
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<ISubscriptionApiService>(),
            sp.GetRequiredService<IConfirmDialogService>()));

        services.AddSingleton<TaskEditorViewModel>(sp => new TaskEditorViewModel(
            sp.GetRequiredService<IDailyActivityApiService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IToastService>(),
            sp.GetRequiredService<ISubscriptionApiService>()));
    }
}