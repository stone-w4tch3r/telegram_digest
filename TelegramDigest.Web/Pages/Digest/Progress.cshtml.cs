using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digest;

public class ProgressModel(BackendClient backend) : BasePageModel
{
    public DigestProgressViewModel? Progress { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Progress = await backend.GetDigestProgress(id);
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load digest progress: {ex.Message}";
            return RedirectToPage("/Digests/Index");
        }
    }
}
