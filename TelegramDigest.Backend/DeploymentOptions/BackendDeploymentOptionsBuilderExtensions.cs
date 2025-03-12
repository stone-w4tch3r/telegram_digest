using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TelegramDigest.Backend.DeploymentOptions;

public static class BackendDeploymentOptionsBuilderExtensions
{
    public static IHostApplicationBuilder AddBackendDeploymentOptions(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddOptionsWithValidateOnStart<BackendDeploymentOptions>()
            .Configure(options =>
            {
                options.MaxConcurrentAiTasks =
                    builder.Configuration.GetValue<int?>(
                        BackendDeploymentOptions.MAX_CONCURRENT_AI_TASKS_KEY
                    ) ?? options.MaxConcurrentAiTasks;
            })
            .ValidateDataAnnotations();

        return builder;
    }
}
