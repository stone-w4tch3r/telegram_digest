using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models;

namespace TelegramDigest.Web.DeploymentOptions;

public record WebDeploymentOptions
{
    public const string BASEPATH_KEY = "BASEPATH";
    public const string HOSTNAME_KEY = "HOSTNAME";

    [UrlBasePath]
    public string BasePath { get; set; } = "/";

    [ModelBinder(typeof(HostModelBinder))]
    public string? HostName { get; set; }
}
