using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public sealed class ProgressModel(BackendClient backend) : BasePageModel
{
    public DigestProgressViewModel? Progress { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await backend.GetDigestProgress(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }

        Progress = result.Value;
        return Page();
    }
}
