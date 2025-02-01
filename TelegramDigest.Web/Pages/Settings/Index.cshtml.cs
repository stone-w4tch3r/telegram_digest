using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly MainServiceClient _mainService;

    public IndexModel(MainServiceClient mainService)
    {
        _mainService = mainService;
    }

    [BindProperty]
    public SettingsViewModel Settings { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Settings = await _mainService.GetSettingsAsync();
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
            return Page();
        }

        try
        {
            await _mainService.UpdateSettingsAsync(Settings);
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
