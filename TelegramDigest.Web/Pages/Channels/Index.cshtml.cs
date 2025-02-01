using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Channels;

public class IndexModel : PageModel
{
    private readonly MainServiceClient _mainService;

    public IndexModel(MainServiceClient mainService)
    {
        _mainService = mainService;
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
            Channels = await _mainService.GetChannelsAsync();
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
            await _mainService.AddChannelAsync(NewChannel);
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
            await _mainService.DeleteChannelAsync(id);
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
            await _mainService.RefreshChannelAsync(id);
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
