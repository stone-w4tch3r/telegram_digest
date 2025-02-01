using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

public class IndexModel(BackendClient backend) : PageModel
{
    public List<ChannelViewModel> Channels { get; set; } = [];

    [BindProperty]
    public AddChannelViewModel? NewChannel { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Channels = await backend.GetChannelsAsync();
            Channels = Channels.OrderBy(c => c.Title).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load channels: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid || NewChannel == null)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            await backend.AddChannelAsync(NewChannel);
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
