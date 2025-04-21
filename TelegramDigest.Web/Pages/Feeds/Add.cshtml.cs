using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Feeds;

public sealed class AddModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    [Url]
    public required string FeedUrl { get; set; }

    public List<RssProvider> RssProviders { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        RssProviders = await backend.GetRssProviders();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            RssProviders = await backend.GetRssProviders();
            return Page();
        }

        try
        {
            await backend.AddOrUpdateFeed(new() { FeedUrl = FeedUrl });

            SuccessMessage = $"Feed '{FeedUrl}' added successfully";
            return RedirectToPage("/Feeds/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add feed: {ex.Message}";
            RssProviders = await backend.GetRssProviders();
            return Page();
        }
    }
}
