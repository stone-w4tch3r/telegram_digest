using System.Diagnostics;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TelegramDigest.Web.Services;

namespace TelegramDigest.Web.Controllers;

public class RssController(
    BackendClient backendClient,
    IMemoryCache cache,
    ILogger<RssController> logger
) : Controller
{
    private const string RSS_CONTENT_TYPE = "application/rss+xml";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
    private const int CACHE_DURATION_SECONDS = 3600; // 1 hour in seconds

    [HttpGet("/rss")]
    [Produces(RSS_CONTENT_TYPE)]
    [ResponseCache(
        Duration = CACHE_DURATION_SECONDS,
        Location = ResponseCacheLocation.Any,
        NoStore = false
    )]
    public async Task<IActionResult> GetFeed()
    {
        const string CacheKey = "RssFeed";

        var isNoCache = Request
            .Headers.CacheControl.ToString()
            .Split(',')
            .Any(h => h.Trim().Equals("no-cache", StringComparison.OrdinalIgnoreCase));

        if (isNoCache)
        {
            return Content(
                SerializeRssFeed(await backendClient.GetRssFeed()),
                RSS_CONTENT_TYPE,
                Encoding.UTF8
            );
        }

        if (!cache.TryGetValue(CacheKey, out string? feed))
        {
            var newFeed = await backendClient.GetRssFeed();

            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(_cacheDuration);

            feed = SerializeRssFeed(newFeed);
            cache.Set(CacheKey, feed, cacheOptions);
        }

        if (feed == null)
        {
            logger.LogError("Missing RSS feed content in cache. This should not happen");
            throw new UnreachableException("Error in RSS loading");
        }

        return Content(feed, RSS_CONTENT_TYPE, Encoding.UTF8);
    }

    private static string SerializeRssFeed(SyndicationFeed feed)
    {
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(
            stringWriter,
            new() { Indent = true, Encoding = Encoding.UTF8 }
        );

        var rssFormatter = new Rss20FeedFormatter(feed, false);
        rssFormatter.WriteTo(xmlWriter);
        xmlWriter.Flush();

        return stringWriter.ToString();
    }
}
