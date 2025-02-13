using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.Database;

namespace TelegramDigest.Backend;

[UsedImplicitly]
public static class ServiceCollectionExtensions
{
    public static void AddTelegramDigest(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ConfigureServices(services, configuration);
    }

    public static async Task InitializeTelegramDigest(this IServiceProvider serviceProvider)
    {
        await InitializeDbAsync(serviceProvider);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
        );

        // Application services
        services.AddScoped<IChannelReader, ChannelReader>();
        services.AddScoped<IChannelsService, ChannelsService>();
        services.AddScoped<IChannelsRepository, ChannelsRepository>();
        services.AddScoped<IDigestsService, DigestsService>();
        services.AddScoped<IDigestRepository, DigestRepository>();
        services.AddScoped<ISummaryGenerator, SummaryGenerator>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IMainService, MainService>();
        services.AddScoped<IRssService, RssService>();
        services.AddScoped<ISettingsManager, SettingsManager>(sp =>
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
