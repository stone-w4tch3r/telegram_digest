using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TelegramDigest.Web.Infrastructure.Auth;

public sealed class SingleUserAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SCHEME_NAME = "SingleUser";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Always sign in as a single dummy user
        var claims = (Claim[])
            [
                new(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
                new(ClaimTypes.Email, "singleuser@localhost"),
            ];
        var identity = new ClaimsIdentity(claims, SCHEME_NAME);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SCHEME_NAME);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
