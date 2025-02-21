using TelegramDigest.Web.Models.ViewModels;
using TelegramDigest.Web.Pages.Shared;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Pages;

public class IndexModel(BackendClient backend, ILogger<IndexModel> logger) : BasePageModel
{
    public DigestSummaryViewModel? LatestDigest { get; set; }
    public int TotalChannels { get; set; }
    public int TotalDigests { get; set; }
    public DateTime? NextDigestTime { get; set; }
    public List<ChannelViewModel> RandomChannels { get; set; } = [];

    public async Task OnGetAsync()
    {
        try
        {
            // Get latest digest
            var digests = await backend.GetDigestSummaries();
            LatestDigest = digests.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            TotalDigests = digests.Count;

            // Get channels info
            var channels = await backend.GetChannels();
            TotalChannels = channels.Count;
            RandomChannels = channels.OrderByDescending(_ => Random.Shared.Next()).Take(5).ToList();

            // Get next digest time from settings
            var settings = await backend.GetSettings();
            NextDigestTime = DateTime.UtcNow.Date.Add(settings.DigestTimeUtc.ToTimeSpan());
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
