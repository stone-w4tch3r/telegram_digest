namespace TelegramDigest.Web.DeploymentOptions;

public static class BuilderExtensions
{
    public static WebApplicationBuilder AddDeploymentOptions(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddOptionsWithValidateOnStart<DeploymentOptions>()
            .Configure(options =>
            {
                options.BasePath =
                    builder.Configuration.GetValue<string>(DeploymentOptions.BASEPATH_KEY)
                    ?? options.BasePath;
                options.HostName =
                    builder.Configuration.GetValue<string>(DeploymentOptions.HOSTNAME_KEY)
                    ?? options.HostName;
            })
            .ValidateDataAnnotations();

        return builder;
    }
}
