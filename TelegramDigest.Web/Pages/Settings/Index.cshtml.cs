using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Settings;

public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public SettingsViewModel? Settings { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await backend.GetSettings();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }
        Settings = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            //TODO error handling
            return Page();
        }
        if (Settings is null)
        {
            throw new UnreachableException("Error in web page! Can't read new settings");
        }

        var result = await backend.UpdateSettings(Settings);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }
        SuccessMessage = "Settings updated successfully";
        return RedirectToPage();
    }
}
