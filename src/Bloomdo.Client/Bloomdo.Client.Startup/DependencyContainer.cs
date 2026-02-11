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
        services.AddSingleton<IAuthApiService, AuthApiService>();
        services.AddSingleton<IAccessTokenManager, AccessTokenManager>();

        // Authorization service
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Toast system
        services.AddSingleton<ToastManager>();
        services.AddSingleton<IToastService, ToastService>();

        // NavigationService requires ShellViewModel, use factory to avoid circular dependency
        services.AddSingleton<INavigationService>(sp =>
        {
            var shellViewModel = sp.GetRequiredService<ShellViewModel>();
            var authorizationService = sp.GetRequiredService<IAuthorizationService>();
            var toastService = sp.GetRequiredService<IToastService>();
            return new NavigationService(sp, authorizationService, toastService, shellViewModel);
        });
    }

    private static void RegisterRepositories(IServiceCollection services)
    {

    }

    private static void RegisterHttpClients(IServiceCollection services)
    {
        services.AddTransient<AuthHeaderHandler>();

        var apiBaseUrl = "https://10.0.2.2:7270/";

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
        // services.AddHttpClient<IOtherApiService, OtherApiService>(...)
        //     .AddHttpMessageHandler<AuthHeaderHandler>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>(sp =>
        {
            var tokenManager = sp.GetRequiredService<IAccessTokenManager>();
            return new ShellViewModel(tokenManager, () => sp.GetRequiredService<INavigationService>());
        });
        
        // Auth views
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<AccessDeniedViewModel>();
        
        // Onboarding components
        services.AddSingleton<OnboardingViewModel>();
        services.AddTransient<WelcomeStepViewModel>();
        services.AddTransient<AskNameStepViewModel>();
        services.AddTransient<SetGoalsStepViewModel>();
        
        // Main view and tabs
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<BlocksViewModel>();
        services.AddSingleton<StatsViewModel>();
        services.AddSingleton<ProfileViewModel>();
    }
}