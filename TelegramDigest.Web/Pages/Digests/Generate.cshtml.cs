using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages.Digests;

public sealed class GenerateModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public DigestGenerationViewModel? Form { get; set; }

    public List<ChannelViewModel> Channels { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var settings = await backend.GetSettings();
        var currentUtc = DateTime.UtcNow;
        var digestTimeToday = currentUtc.Date.Add(settings.DigestTimeUtc.ToTimeSpan());

        Channels = await backend.GetChannels();

        // If digest time hasn't passed today, set range to previous day
        // Otherwise set range to today
        Form = new()
        {
            DateTo = currentUtc >= digestTimeToday ? currentUtc.Date.AddDays(1) : currentUtc.Date,
            DateFrom =
                currentUtc >= digestTimeToday ? currentUtc.Date : currentUtc.Date.AddDays(-1),
            SelectedChannels = [.. Channels.Select(c => c.TgId.ToString())],
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Channels = await backend.GetChannels();
            return Page();
        }

        if (Form == null)
        {
            ErrorMessage = "Form is null. Error in frontend!";
            Channels = await backend.GetChannels();
            return Page();
        }

        try
        {
            var digestId = await backend.QueueDigest(Form);
            return RedirectToPage("Progress", new { id = digestId });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to queue digest: {ex.Message}";
            Channels = await backend.GetChannels();
            return Page();
        }
    }
}
