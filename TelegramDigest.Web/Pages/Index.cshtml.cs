using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages;

public class IndexModel(MainServiceClient mainService, ILogger<IndexModel> logger) : PageModel
{
    public DigestSummaryViewModel? LatestDigest { get; set; }
    public int TotalChannels { get; set; }
    public int TotalDigests { get; set; }
    public DateTime? NextDigestTime { get; set; }
    public List<ChannelViewModel> RecentChannels { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            // Get latest digest
            var digests = await mainService.GetDigestsAsync();
            LatestDigest = digests.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            TotalDigests = digests.Count;

            // Get channels info
            var channels = await mainService.GetChannelsAsync();
            TotalChannels = channels.Count;
            RecentChannels = channels.OrderByDescending(c => c.LastUpdate).Take(5).ToList();

            // Get next digest time from settings
            var settings = await mainService.GetSettingsAsync();
            NextDigestTime = DateTime.UtcNow.Date.Add(settings.DigestTimeUtc);
            if (NextDigestTime < DateTime.UtcNow)
            {
                NextDigestTime = NextDigestTime.Value.AddDays(1);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading dashboard data");
        }
    }
}
