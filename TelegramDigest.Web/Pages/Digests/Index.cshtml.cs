using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public List<DigestSummaryViewModel> Digests { get; set; } = new();

    public async Task OnGetAsync()
    {
        Digests = await backend.GetDigestSummaries();
        Digests = Digests.OrderByDescending(d => d.CreatedAt).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await backend.DeleteDigest(id);
            SuccessMessage = "Digest deleted successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete digest: {ex.Message}";
            return RedirectToPage();
        }
    }
}
