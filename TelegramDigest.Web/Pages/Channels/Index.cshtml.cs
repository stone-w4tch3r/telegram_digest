using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

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
        try
        {
            var feeds = await backend.GetFeeds();
            Feeds = feeds.OrderBy(c => c.Title).Select(FeedWithMetadata.FromFeedViewModel).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load feeds: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string feedUrl)
    {
        try
        {
            await backend.DeleteFeedAsync(feedUrl);
            SuccessMessage = "Feed deleted successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete feed: {ex.Message}";
            return RedirectToPage();
        }
    }
}