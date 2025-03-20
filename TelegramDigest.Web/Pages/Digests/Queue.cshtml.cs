using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public class QueueModel(BackendClient backend) : BasePageModel
{
    public Guid[] InProgressDigests { get; private set; } = [];
    public Guid[] WaitingDigests { get; private set; } = [];

    public async Task OnGetAsync()
    {
        InProgressDigests = await backend.GetInProgressDigests();
        WaitingDigests = await backend.GetWaitingDigests();
    }

    public async Task<IActionResult> OnPostCancelDigestAsync(Guid digestId)
    {
        try
        {
            await backend.CancelDigest(digestId);
        }
        catch (Exception ex)
        {
            // TODO fixme
            ErrorMessage = $"Failed to cancel digest: {ex.Message}";
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveWaitingDigestAsync(Guid digestId)
    {
        try
        {
            await backend.RemoveWaitingDigest(digestId);
        }
        catch (Exception ex)
        {
            // TODO fixme
            ErrorMessage = $"Failed to remove waiting digest: {ex.Message}";
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }
        return RedirectToPage();
    }
}
