using Bloomdo.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDatabaseContext(this IServiceCollection serviceCollection, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is empty or null.", nameof(connectionString));
        }

        serviceCollection.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        
    }

    public static void RegisterRepositories(this IServiceCollection services)
    {
       
    }
}