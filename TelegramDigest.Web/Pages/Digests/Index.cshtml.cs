using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public class IndexModel(BackendClient backend) : BasePageModel
{
    public List<DigestSummaryViewModel> Digests { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Digests = await backend.GetDigestsAsync();
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
            await backend.GenerateDigestAsync();
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
