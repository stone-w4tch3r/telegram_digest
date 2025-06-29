using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TelegramDigest.Web.Infrastructure.Auth;
using TelegramDigest.Web.Options;

namespace TelegramDigest.Web.Controllers;

[Route("Auth")]
public sealed class AuthController(IOptions<AuthOptions> authOptions) : Controller
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_authOptions.Mode == AuthMode.SingleUser)
        {
            HttpContext.Session.SetString(
                SingleUserAuthHandler.LOGIN_COOKIE,
                SingleUserAuthHandler.LOGIN_COOKIE_VALUE
            );
            return Redirect(returnUrl ?? Url.Content("~/"));
        }
        return _authOptions.Mode switch
        {
            AuthMode.OpenIdConnect => Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                OpenIdConnectDefaults.AuthenticationScheme
            ),
            _ => Redirect(returnUrl ?? Url.Content("~/")),
        };
    }

    [HttpGet("Logout")]
    public IActionResult Logout(string? returnUrl = null)
    {
        if (_authOptions.Mode == AuthMode.SingleUser)
        {
            HttpContext.Session.Remove(SingleUserAuthHandler.LOGIN_COOKIE);
            return Redirect(Url.Content("~/"));
        }

        if (_authOptions.Mode == AuthMode.ReverseProxy)
        {
            if (string.IsNullOrWhiteSpace(_authOptions.ReverseProxyLogoutUrl))
            {
                throw new UnreachableException(
                    $"No {_authOptions.ReverseProxyLogoutUrl} for reverse proxy mode. Auth is misconfigured and early validation did not catch it"
                );
            }

            return Redirect(_authOptions.ReverseProxyLogoutUrl);
        }

        if (_authOptions.Mode == AuthMode.OpenIdConnect)
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        throw new UnreachableException($"Unknown auth mode: {_authOptions.Mode}");
    }
}
