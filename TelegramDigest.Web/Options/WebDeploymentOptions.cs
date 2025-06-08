using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RuntimeNullables;
using TelegramDigest.Web.Models;
using Host = TelegramDigest.Types.Host.Host;

namespace TelegramDigest.Web.Options;

[NullChecks(false)]
public sealed record WebDeploymentOptions
{
    [UrlBasePath(memberName: "BASEPATH")]
    [Required(ErrorMessage = "BASEPATH configuration option was not set")]
    [ConfigurationKeyName("BASEPATH")]
    public required string BasePath { get; set; }

    [ModelBinder(typeof(HostModelBinder))] // TODO not called
    [Required(ErrorMessage = "HOSTNAME configuration option was not set")]
    [ConfigurationKeyName("HOSTNAME")]
    public required Host HostName { get; set; }
}
