using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace TelegramDigest.Web.Options;

/// <summary>
/// Authentication configuration options for TelegramDigest.
/// Controls SingleUser, Reverse Proxy, and OpenID Connect authentication modes.
/// </summary>
[NullChecks(false)]
[AuthOptionsConsistency]
internal record AuthenticationOptions
{
    /// <summary>
    /// If true, enables dummy local authentication (single-user mode). All other auth fields must be unset.
    /// </summary>
    [Required(ErrorMessage = "SINGLE_USER_MODE configuration option was not set")]
    [ConfigurationKeyName("SINGLE_USER_MODE")]
    public required bool SingleUserMode { get; init; }

    /// <summary>
    /// OpenID Connect authority URL. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("AUTHORITY")]
    public string? Authority { get; init; }

    /// <summary>
    /// OpenID Connect client ID. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("CLIENT_ID")]
    public string? ClientId { get; init; }

    /// <summary>
    /// OpenID Connect client secret. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("CLIENT_SECRET")]
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Name of the HTTP header containing the user's email. Required in reverse proxy mode.
    /// </summary>
    [ConfigurationKeyName("PROXY_HEADER_EMAIL")]
    public string? ProxyHeaderEmail { get; init; }

    /// <summary>
    /// Name of the HTTP header containing the user's unique ID. Required in reverse proxy mode.
    /// </summary>
    [ConfigurationKeyName("PROXY_HEADER_ID")]
    public string? ProxyHeaderId { get; init; }

    /// <summary>
    /// Cookie name for ASP authentication cookie. Optional; default is used if unset. Can be configured for any mode.
    /// </summary>
    [ConfigurationKeyName("COOKIE_NAME")]
    public string? CookieName { get; init; }
}
