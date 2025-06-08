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
        var result = await backend.GetDigestSummaries();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return;
        }
        Digests = result.Value.OrderByDescending(d => d.CreatedAt).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var result = await backend.DeleteDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage();
        }
        SuccessMessage = "Digest deleted successfully";
        return RedirectToPage();
    }
}
