using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

public class IndexModel(BackendClient backend) : BasePageModel
{
    public List<ChannelViewModel> Channels { get; set; } = [];

    [BindProperty]
    public AddChannelViewModel? NewChannel { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Channels = await backend.GetChannels();
            Channels = Channels.OrderBy(c => c.Title).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load channels: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }
        if (NewChannel == null)
        {
            await OnGetAsync();
            ErrorMessage = "Error in web page! Can't read new channel data";
            return Page();
        }

        try
        {
            await backend.AddOrUpdateChannel(NewChannel);
            SuccessMessage = $"Channel '{NewChannel.TgId}' added successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add channel: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string tgId)
    {
        try
        {
            await backend.DeleteChannelAsync(tgId);
            SuccessMessage = "Channel deleted successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete channel: {ex.Message}";
            return RedirectToPage();
        }
    }
}
