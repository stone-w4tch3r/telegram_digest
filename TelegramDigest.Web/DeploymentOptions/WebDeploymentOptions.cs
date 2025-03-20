using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models;

namespace TelegramDigest.Web.DeploymentOptions;

public record WebDeploymentOptions
{
    public const string BASEPATH_KEY = "BASEPATH";
    public const string HOSTNAME_KEY = "HOSTNAME";

    [UrlBasePath(memberName: BASEPATH_KEY)]
    [Required(ErrorMessage = $"{BASEPATH_KEY} configuration option was not set")]
    [NotNull]
    public string? BasePath { get; set; }

    [ModelBinder(typeof(HostModelBinder))]
    public string? HostName { get; set; }
}
