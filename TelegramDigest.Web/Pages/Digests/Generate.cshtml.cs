using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Backend.Core;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public sealed class GenerateModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public DigestGenerationViewModel? Form { get; set; }

    public List<FeedViewModel> Feeds { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var settings = await backend.GetSettings();
        var currentUtc = DateTime.UtcNow;
        var digestTimeToday = currentUtc.Date.Add(settings.DigestTimeUtc.ToTimeSpan());

        Feeds = await backend.GetFeeds();

        // If digest time hasn't passed today, set range to previous day
        // Otherwise set range to today
        Form = new()
        {
            DateTo = currentUtc >= digestTimeToday ? currentUtc.Date.AddDays(1) : currentUtc.Date,
            DateFrom =
                currentUtc >= digestTimeToday ? currentUtc.Date : currentUtc.Date.AddDays(-1),
            SelectedFeeds = Feeds.Select(f => new FeedUrl(f.Url)).ToArray(),
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Feeds = await backend.GetFeeds();
            return Page();
        }

        if (Form == null)
        {
            ErrorMessage = "Form is null. Error in frontend!";
            Feeds = await backend.GetFeeds();
            return Page();
        }

        try
        {
            var digestId = await backend.QueueDigest(Form);
            SuccessMessage = "Digest generation queued successfully";
            return RedirectToPage("/Digest/Progress", new { id = digestId });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to queue digest: {ex.Message}";
            Feeds = await backend.GetFeeds();
            return Page();
        }
    }
}
