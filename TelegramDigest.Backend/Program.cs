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

    public static async Task UseTelegramDigest(this IServiceProvider serviceProvider)
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
        services.AddHostedService<QueueProcessorBackgroundService>();
        services.AddHostedService<SchedulerBackgroundService>();
        services.AddScoped<IChannelReader, ChannelReader>();
        services.AddScoped<IChannelsService, ChannelsService>();
        services.AddScoped<IChannelsRepository, ChannelsRepository>();
        services.AddScoped<IDigestService, DigestService>();
        services.AddScoped<IDigestService, DigestService>();
        services.AddScoped<IDigestRepository, DigestRepository>();
        services.AddScoped<IDigestStepsService, DigestStepsService>();
        services.AddScoped<IDigestStepsRepository, DigestStepsRepository>();
        services.AddScoped<IAiSummarizer, AiSummarizer>();
        services.AddSingleton<ITaskQueue, TaskQueue>();
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
