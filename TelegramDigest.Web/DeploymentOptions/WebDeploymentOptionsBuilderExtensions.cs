namespace TelegramDigest.Web.DeploymentOptions;

public static class WebDeploymentOptionsBuilderExtensions
{
    public static IHostApplicationBuilder AddWebDeploymentOptions(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddOptionsWithValidateOnStart<WebDeploymentOptions>()
            .Configure(options =>
            {
                options.BasePath = builder.Configuration.GetValue<string>(
                    WebDeploymentOptions.BASEPATH_KEY
                );
                options.HostName = builder.Configuration.GetValue<string>(
                    WebDeploymentOptions.HOSTNAME_KEY
                );
            })
            .ValidateDataAnnotations();

        return builder;
    }
}
