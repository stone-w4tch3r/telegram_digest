using System.Diagnostics;
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
        var settingsResult = await backend.GetSettings();
        if (settingsResult.IsFailed)
        {
            Errors = settingsResult.Errors;
            return;
        }
        var settings = settingsResult.Value;
        var currentUtc = DateTime.UtcNow;
        var digestTimeToday = currentUtc.Date.Add(settings.DigestTimeUtc.ToTimeSpan());

        var feedsResult = await backend.GetFeeds();
        if (feedsResult.IsFailed)
        {
            Errors = feedsResult.Errors;
            return;
        }
        Feeds = feedsResult.Value;

        // If digest time hasn't passed today, set range to previous day
        // Otherwise set range to today
        var allFeedUrls = Feeds.Select(f => f.Url).ToArray();
        Form = new()
        {
            DateTo = currentUtc >= digestTimeToday ? currentUtc.Date.AddDays(1) : currentUtc.Date,
            DateFrom =
                currentUtc >= digestTimeToday ? currentUtc.Date : currentUtc.Date.AddDays(-1),
            SelectedFeedUrls = allFeedUrls,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var feedsResult = await backend.GetFeeds();
            if (!feedsResult.IsFailed)
            {
                Feeds = feedsResult.Value;
            }

            return Page();
        }

        if (Form == null)
        {
            throw new UnreachableException("Form is null. Error in frontend!");
        }

        var result = await backend.QueueDigest(Form.DateFrom, Form.DateTo, Form.SelectedFeedUrls);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            var feedsResult = await backend.GetFeeds();
            if (!feedsResult.IsFailed)
            {
                Feeds = feedsResult.Value;
            }

            return Page();
        }
        SuccessMessage = "Digest generation queued successfully";
        return RedirectToPage("/Digest/Progress", new { id = result.Value });
    }
}
