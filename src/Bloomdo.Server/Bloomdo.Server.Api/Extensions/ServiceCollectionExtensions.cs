using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Infrastructure.Data;
using Bloomdo.Server.Infrastructure.Data.Repositories;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Server.Infrastructure.Services;
using Bloomdo.Server.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Bloomdo.Server.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public void AddDatabaseContext(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is empty or null.", nameof(connectionString));
            }

            serviceCollection.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        public void AddJwtAuthentication(JwtSettings jwtSettings)
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            serviceCollection.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[JWT] Auth FAILED: {context.Exception.GetType().Name}: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var sub = context.Principal?.FindFirst("sub")?.Value;
                            Console.WriteLine($"[JWT] Token validated for sub={sub}");
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Console.WriteLine($"[JWT] Challenge: Error={context.Error}, Description={context.ErrorDescription}, AuthFailure={context.AuthenticateFailure?.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });

            // Permission-based authorization
            serviceCollection.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            serviceCollection.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            serviceCollection.AddAuthorization();

            serviceCollection.AddSingleton(jwtSettings);
        }

        public void RegisterServices()
        {
            serviceCollection.AddScoped<IJwtService, JwtService>();
            serviceCollection.AddScoped<IAuthService, AuthService>();
            serviceCollection.AddScoped<IAuthSettings>(sp => sp.GetRequiredService<JwtSettings>());
            serviceCollection.AddScoped<IStatsService, StatsService>();
            serviceCollection.AddScoped<IBlockService, BlockService>();
            serviceCollection.AddScoped<IAchievementService, AchievementService>();
        }

        public void RegisterRepositories()
        {
            serviceCollection.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            serviceCollection.AddScoped<IAccountRepository, AccountRepository>();
            serviceCollection.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            serviceCollection.AddScoped<IStatsRepository, StatsRepository>();
        }
    }
}