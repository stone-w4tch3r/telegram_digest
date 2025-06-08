using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Feeds;

public sealed record FeedWithMetadata(FeedViewModel Feed, bool IsTelegramFeed)
{
    public static FeedWithMetadata FromFeedViewModel(FeedViewModel feed) =>
        new(feed, feed.Url.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase));
}

public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public List<FeedWithMetadata>? Feeds { get; set; }

    public async Task OnGetAsync()
    {
        var result = await backend.GetFeeds();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return;
        }
        Feeds = result
            .Value.OrderBy(c => c.Title)
            .Select(FeedWithMetadata.FromFeedViewModel)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string feedUrl)
    {
        var result = await backend.DeleteFeedAsync(feedUrl);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage();
        }
        SuccessMessage = "Feed deleted successfully";
        return RedirectToPage();
    }
}
