using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

public class IndexModel : PageModel
{
    private readonly BackendClient _backend;

    public IndexModel(BackendClient backend)
    {
        _backend = backend;
    }

    public List<ChannelViewModel> Channels { get; set; } = new();

    [BindProperty]
    public AddChannelViewModel NewChannel { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Channels = await _backend.GetChannelsAsync();
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

        try
        {
            await _backend.AddChannelAsync(NewChannel);
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

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _backend.DeleteChannelAsync(id);
            SuccessMessage = "Channel deleted successfully";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete channel: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRefreshAsync(int id)
    {
        try
        {
            await _backend.RefreshChannelAsync(id);
            SuccessMessage = "Channel refresh initiated";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh channel: {ex.Message}";
            return RedirectToPage();
        }
    }
}
