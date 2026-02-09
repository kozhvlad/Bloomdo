using Bloomdo.Application.Services;
using Bloomdo.Application.ViewModels;
using Bloomdo.Application.ViewModels.OnbordingComponents;
using Bloomdo.Application.ViewModels.MainComponents;
using Bloomdo.Core.Interfaces;
using Bloomdo.Infrastructure.Middleware;
using Bloomdo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Bloomdo.Infrastructure.DatabaseContexts;

namespace Bloomdo.Startup;

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
        services.AddSingleton<IAccessTokenManager, AccessTokenManager>();
        services.AddSingleton<INavigationService, NavigationService>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {

    }

    private static void RegisterHttpClients(IServiceCollection services)
    {
        services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://test.com/api/");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();
        
        // Onboarding components
        services.AddSingleton<OnboardingViewModel>();
        services.AddTransient<LoginViewModel>();
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