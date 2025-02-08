using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public DigestSummaryViewModel? Summary { get; set; }

    public PostSummaryViewModel[]? Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            if (id != Id)
            {
                throw new UnreachableException(
                    "Error in frontend! Id of digest does not match the one in the URL"
                );
            }

            var digestResult = await backend.GetDigestAsync(id);
            if (digestResult is null)
            {
                return Page();
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
