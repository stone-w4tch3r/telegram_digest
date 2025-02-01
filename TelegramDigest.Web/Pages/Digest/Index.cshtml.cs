using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public class IndexModel : PageModel
{
    private readonly BackendClient _backend;

    public IndexModel(BackendClient backend)
    {
        _backend = backend;
    }

    public DigestSummaryViewModel? Digest { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Digest = await _backend.GetDigestAsync(id);

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
