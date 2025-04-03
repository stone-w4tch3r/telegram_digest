using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.Db;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Backend;

public static class ServiceCollectionExtensions
{
    public static void AddBackend(this IHostApplicationBuilder builder)
    {
        // Deployment options
        builder.AddBackendDeploymentOptions();

        // Application services
        builder.Services.AddHostedService<DigestProcessor>();
        builder.Services.AddHostedService<SchedulerBackgroundService>();
        builder.Services.AddHostedService<DigestStepsProcessor>();

        // Scoped
        builder.Services.AddScoped<IChannelReader, ChannelReader>();
        builder.Services.AddScoped<IChannelsService, ChannelsService>();
        builder.Services.AddScoped<IChannelsRepository, ChannelsRepository>();
        builder.Services.AddScoped<IDigestService, DigestService>();
        builder.Services.AddScoped<IDigestService, DigestService>();
        builder.Services.AddScoped<IDigestRepository, DigestRepository>();
        builder.Services.AddScoped<IDigestStepsService, DigestStepsService>();
        builder.Services.AddScoped<IDigestStepsRepository, DigestStepsRepository>();
        builder.Services.AddScoped<IAiSummarizer, AiSummarizer>();
        builder.Services.AddScoped<IEmailSender, EmailSender>();
        builder.Services.AddScoped<ISettingsManager, FileSettingsManager>();
        builder.Services.AddScoped<IMainService, MainService>();
        builder.Services.AddScoped<IRssService, RssService>();
        builder.Services.AddScoped<IDigestProcessingOrchestrator, DigestProcessingOrchestrator>();

        // Singletons
        builder.Services.AddSingleton<IDigestStepsChannel, DigestStepsChannel>();
        builder.Services.AddSingleton<TaskManager<DigestId>>();
        builder.Services.AddSingleton<ITaskScheduler<DigestId>>(provider =>
            provider.GetRequiredService<TaskManager<DigestId>>()
        );
        builder.Services.AddSingleton<ITaskTracker<DigestId>>(sp =>
            sp.GetRequiredService<TaskManager<DigestId>>()
        );

        // Database
        builder.Services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, options) =>
            {
                var deploymentOptions = serviceProvider
                    .GetRequiredService<IOptions<BackendDeploymentOptions>>()
                    .Value;
                options.UseSqlite(deploymentOptions.SqlLiteConnectionString);
            }
        );

        // Logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
        });
    }

    public static async Task UseBackend(this IServiceProvider services)
    {
        // Database initial migration
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}
