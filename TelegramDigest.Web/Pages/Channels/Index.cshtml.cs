using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public List<FeedViewModel>? Feeds { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Feeds = await backend.GetFeeds();
            Feeds = Feeds.OrderBy(c => c.Title).ToList();
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
