using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TelegramDigest.Web.Options;

namespace TelegramDigest.Web.Controllers;

[Route("Auth")]
public sealed class AuthController(IOptions<AuthOptions> authOptions) : Controller
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
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
        return _authOptions.Mode switch
        {
            AuthMode.OpenIdConnect => SignOut(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ),
            AuthMode.ReverseProxy => SignOut(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                CookieAuthenticationDefaults.AuthenticationScheme
            ),
            AuthMode.SingleUser => Redirect(returnUrl ?? Url.Content("~/")),
            _ => throw new UnreachableException($"Unknown auth mode: {_authOptions.Mode}"),
        };
    }
}
