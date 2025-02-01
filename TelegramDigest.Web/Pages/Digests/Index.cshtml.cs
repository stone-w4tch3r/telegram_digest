using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public class IndexModel : PageModel
{
    private readonly BackendClient _backend;

    public IndexModel(BackendClient backend)
    {
        _backend = backend;
    }

    public List<DigestSummaryViewModel> Digests { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Digests = await _backend.GetDigestsAsync();
            Digests = Digests.OrderByDescending(d => d.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load digests: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        try
        {
            await _backend.GenerateDigestAsync();
            SuccessMessage = "Digest generation started successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to generate digest: {ex.Message}";
            return RedirectToPage();
        }
    }
}
