using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public class IndexModel : PageModel
{
    private readonly MainServiceClient _mainService;

    public IndexModel(MainServiceClient mainService)
    {
        _mainService = mainService;
    }

    public DigestSummaryViewModel? Digest { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Digest = await _mainService.GetDigestAsync(id);

            if (Digest == null)
            {
                return NotFound();
            }

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load digest: {ex.Message}";
            return RedirectToPage("/Digests/Index");
        }
    }
}
