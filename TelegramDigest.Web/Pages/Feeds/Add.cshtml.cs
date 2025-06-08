using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using RuntimeNullables;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Feeds;

[RequestTimeout(10)]
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

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        RssProviders = await backend.GetRssProviders(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
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

        RssProviders = await backend.GetRssProviders(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var feedUrl = Type switch
        {
            FeedType.DirectRss when DirectRss != null => DirectRss.FeedUrl,
            FeedType.Telegram when Telegram != null => RssProviders
                .Single(p => p.Id == Telegram.ProviderId)
                .BaseUrl + Telegram.ChannelId,
            _ => throw new UnreachableException("Invalid form state"),
        };

        var result = await backend.AddOrUpdateFeed(new() { FeedUrl = feedUrl }, ct);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }
        SuccessMessage = $"Feed '{feedUrl}' added successfully";
        return RedirectToPage("/Channels/Index");
    }
}

[NullChecks(false)]
public sealed record DirectRssFeedModel
{
    [Url]
    [Display(Name = "RSS Feed URL")]
    public required string FeedUrl { get; init; }
}

[NullChecks(false)]
public sealed record TelegramFeedModel
{
    [Display(Name = "RSS Provider")]
    public required string ProviderId { get; init; }

    [Display(Name = "Channel ID")]
    [RegularExpression(
        "^[a-zA-Z][a-zA-Z0-9_]{4,31}$",
        ErrorMessage = "Channel ID must be 5-32 characters, only letters, numbers, and underscores, starting with a letter"
    )]
    public required string ChannelId { get; init; }
}
