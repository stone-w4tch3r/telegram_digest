using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TelegramDigest.Backend.DeploymentOptions;

internal static class BackendDeploymentOptionsBuilderExtensions
{
    internal static IHostApplicationBuilder AddBackendDeploymentOptions(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddOptionsWithValidateOnStart<BackendDeploymentOptions>()
            .Configure(options =>
            {
                options.MaxConcurrentAiTasks = builder.Configuration.GetValue<int?>(
                    BackendDeploymentOptions.MAX_CONCURRENT_AI_TASKS_KEY
                );
                options.SqlLiteConnectionString = builder.Configuration.GetValue<string?>(
                    BackendDeploymentOptions.SQL_LITE_CONNECTION_STRING_KEY
                );
                options.TelegramRssBaseUrl = builder.Configuration.GetValue<Uri?>(
                    BackendDeploymentOptions.TELEGRAM_RSS_BASE_URL_KEY
                );
            })
            .ValidateDataAnnotations();

        return builder;
    }
}
