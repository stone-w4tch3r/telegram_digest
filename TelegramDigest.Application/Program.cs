using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using TelegramDigest.Application.Database;
using TelegramDigest.Application.Public;
using TelegramDigest.Application.Services;

namespace TelegramDigest.Application;

[UsedImplicitly]
public static class ServiceCollectionExtensions
{
    [UsedImplicitly]
    public static IServiceCollection AddTelegramDigest(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ConfigureServices(services, configuration);
        return services;
    }

    [UsedImplicitly]
    public static async Task<IServiceProvider> InitializeTelegramDigest(
        this IServiceProvider serviceProvider
    )
    {
        await InitializeDbAsync(serviceProvider);
        return serviceProvider;
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
        );

        // OpenAI

        // Application services
        services.AddScoped<ChannelReader>();
        services.AddScoped<ChannelsService>();
        services.AddScoped<ChannelsRepository>();
        services.AddScoped<DigestsService>();
        services.AddScoped<DigestRepository>();
        services.AddScoped<SummaryGenerator>();
        services.AddScoped<EmailSender>();
        services.AddScoped<MainService>();
        services.AddScoped<IMainService, MainService>();
        services.AddScoped<PublicFacade>();
        services.AddScoped<SettingsManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SettingsManager>>();
            var settingsPath = configuration.GetValue<string>("SettingsPath");
            return new(settingsPath, logger);
        });

        // Logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });
    }

    private static async Task InitializeDbAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}
