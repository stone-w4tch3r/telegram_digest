using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TelegramDigest.Web.Pages;

[AllowAnonymous]
public sealed class LandingModel : PageModel
{
    public void OnGet() { }
}
