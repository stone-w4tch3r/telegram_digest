using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Feeds;

public sealed class AddModel(BackendClient backend) : BasePageModel
{
    public enum FeedType
    {
        [Display(Name = "Direct RSS Feed")]
        DirectRss,

        [Display(Name = "Telegram Channel")]
        Telegram,
    }

    [BindProperty]
    public required FeedType? Type { get; set; }

    [BindProperty]
    public DirectRssFeedModel? DirectRss { get; set; }

    [BindProperty]
    public TelegramFeedModel? Telegram { get; set; }

    public List<RssProvider>? RssProviders { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        RssProviders = await backend.GetRssProviders();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Clear validation errors for the model that wasn't selected
        if (Type == FeedType.DirectRss)
        {
            ModelState.Remove("Telegram.ProviderId");
            ModelState.Remove("Telegram.ChannelId");
        }
        else if (Type == FeedType.Telegram)
        {
            ModelState.Remove("DirectRss.FeedUrl");
        }

        if (!ModelState.IsValid)
        {
            return RedirectToPage();
        }

        RssProviders = await backend.GetRssProviders();

        try
        {
            var feedUrl = Type switch
            {
                FeedType.DirectRss when DirectRss != null => DirectRss.FeedUrl,
                FeedType.Telegram when Telegram != null => RssProviders
                    .Single(p => p.Id == Telegram.ProviderId)
                    .BaseUrl + Telegram.ChannelId,
                _ => throw new UnreachableException("Invalid form state"),
            };

            await backend.AddOrUpdateFeed(new() { FeedUrl = feedUrl });
            SuccessMessage = $"Feed '{feedUrl}' added successfully";
            return RedirectToPage("/Channels/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add feed: {ex.Message}";
            RssProviders = await backend.GetRssProviders();
            return RedirectToPage();
        }
    }
}

public sealed class DirectRssFeedModel
{
    [Required]
    [Url]
    [Display(Name = "RSS Feed URL")]
    public required string FeedUrl { get; init; }
}

public sealed class TelegramFeedModel
{
    [Required]
    [Display(Name = "RSS Provider")]
    public required string ProviderId { get; init; }

    [Required]
    [Display(Name = "Channel ID")]
    [RegularExpression(
        "^[a-zA-Z][a-zA-Z0-9_]{4,31}$",
        ErrorMessage = "Channel ID must be 5-32 characters, only letters, numbers, and underscores, starting with a letter"
    )]
    public string? ChannelId { get; init; }
}
