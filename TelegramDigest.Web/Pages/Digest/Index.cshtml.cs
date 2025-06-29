using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public DigestViewModel? Summary { get; set; }

    public PostSummaryViewModel[]? Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id != Id)
        {
            throw new UnreachableException(
                "Error in frontend! Id of digest does not match the one in the URL"
            );
        }

        var result = await backend.GetDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }

        (Summary, Posts) = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var result = await backend.DeleteDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage("/Digests/Index");
        }
        SuccessMessage = "Digest deleted successfully";
        return RedirectToPage("/Digests/Index");
    }
}
