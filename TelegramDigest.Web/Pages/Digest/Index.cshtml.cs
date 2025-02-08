using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public class IndexModel(BackendClient backend) : BasePageModel
{
    public DigestSummaryViewModel? Summary { get; set; }

    public PostSummaryViewModel[]? Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var digestResult = await backend.GetDigestAsync(id);
            if (digestResult is null)
            {
                return NotFound(); // TODO test
            }

            (Summary, Posts) = digestResult.Value;

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load digest: {ex.Message}";
            return RedirectToPage("/Digests/Index");
        }
    }
}
