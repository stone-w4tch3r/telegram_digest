namespace TelegramDigest.Backend.Infrastructure;

public enum AuthenticationMode
{
    SingleUser,
    ReverseProxy,
    OpenIdConnect,
}

/// <summary>
/// Contains the authentication configuration that backend needs to know.
/// </summary>
/// <param name="ProxyHeaderId">Name of the HTTP header containing the user's unique ID, for reverse proxy mode.</param>
/// <param name="Mode">Current authentication mode.</param>
public sealed record BackendAuthenticationConfiguration(
    string? ProxyHeaderId,
    AuthenticationMode Mode
);
