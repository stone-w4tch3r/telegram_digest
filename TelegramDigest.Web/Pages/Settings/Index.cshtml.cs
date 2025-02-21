using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Settings;

public class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public SettingsViewModel? Settings { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Settings = await backend.GetSettings();
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load settings: {ex.Message}";
            return RedirectToPage("/Index");
        }
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
            ErrorMessage = "Error in web page! Can't read new settings";
            return Page();
        }

        try
        {
            await backend.UpdateSettings(Settings);
            SuccessMessage = "Settings updated successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update settings: {ex.Message}";
            return Page();
        }
    }
}
