using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Settings;

public class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty] //TODO what is this?
    public SettingsViewModel? Settings { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Settings = await backend.GetSettingsAsync();
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
        if (!ModelState.IsValid || Settings is null)
        {
            //TODO error handling
            return Page();
        }

        try
        {
            await backend.UpdateSettingsAsync(Settings);
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
