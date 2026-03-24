using Bloomdo.Server.Api.Extensions;
using Bloomdo.Server.Api.Middleware;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Server.Infrastructure.Data;
using Bloomdo.Server.Infrastructure.Settings;
using Microsoft.OpenApi;
using Serilog;

namespace Bloomdo.Server.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Kestrel to listen on all network interfaces for Android emulator access
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(5043);
            serverOptions.ListenAnyIP(7270, listenOptions =>
            {
                listenOptions.UseHttps();
            });
        });

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure JWT Settings
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

        // Configure Gemini AI Settings
        var geminiApiKeys = Enumerable.Range(1, 6)
            .Select(i => builder.Configuration[$"API:Gemini-API-Key-{i}"] ?? string.Empty)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();
        var geminiSettings = new GeminiSettings { ApiKeys = geminiApiKeys };
        builder.Services.AddSingleton<IGeminiSettings>(geminiSettings);

        // Configure Free Limits Settings
        var freeLimitsSettings = builder.Configuration.GetSection("FreeLimits").Get<FreeLimitsSettings>() ?? new FreeLimitsSettings();
        builder.Services.AddSingleton<IFreeLimitsSettings>(freeLimitsSettings);

        // Configure Stripe Settings
        var stripeSettings = new StripeSettings
        {
            PublishableKey = builder.Configuration["Stripe:Stripe_pk"] ?? string.Empty,
            SecretKey = builder.Configuration["Stripe:Stripe_sk"] ?? string.Empty,
            WebhookSecret = builder.Configuration["Stripe:WebhookSecret"],
            MonthlyPriceId = builder.Configuration["Stripe:MonthlyPriceId"] ?? string.Empty,
            YearlyPriceId = builder.Configuration["Stripe:YearlyPriceId"] ?? string.Empty
        };
        builder.Services.AddSingleton<IStripeSettings>(stripeSettings);

        // JWT Authentication & authorization
        builder.Services.AddJwtAuthentication(jwtSettings);

        // Dependency injection
        builder.Services.AddDatabaseContext(builder.Configuration.GetConnectionString("DefaultConnection"));
        builder.Services.RegisterServices();
        builder.Services.RegisterRepositories();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter JWT token (e.g. Bearer eyJ...)",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            // Seed test data for development
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                await DevDataSeeder.SeedAsync(db, logger);
            }
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        await app.RunAsync();
    }
}