using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class ChannelReader(ILogger<ChannelReader> logger)
{
    private const string RssHubBaseUrl = "https://rsshub.app/telegram/channel";

    internal Task<Result<List<PostModel>>> FetchPosts(
        ChannelId channelId,
        DateOnly from,
        DateOnly to
    ) =>
        Task.Run(() =>
        {
            if (from > to)
                return Result.Fail(
                    $"Can't fetch posts, invalid period requested, from: [{from}] to [{to}]"
                );

            try
            {
                var feedUrl = $"{RssHubBaseUrl}/{channelId.ChannelName}";
                using var reader = XmlReader.Create(feedUrl);
                var feed = SyndicationFeed.Load(reader);

                var posts = feed
                    .Items.Where(x =>
                        DateOnly.FromDateTime(x.PublishDate.DateTime) >= from
                        && DateOnly.FromDateTime(x.PublishDate.DateTime) <= to
                    )
                    .Select(x => new PostModel(
                        ChannelId: channelId,
                        HtmlContent: new(x.Summary.Text),
                        Url: x.Links.SingleOrDefault()?.Uri
                            ?? throw new FormatException(
                                $"Telegram Channel RSS item [{x.Id}] does not have a valid URL [{LinksCollectionToString(x.Links)}]"
                            ),
                        PublishedAt: x.PublishDate.DateTime
                    ))
                    .ToList();

                return Result.Ok(posts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching posts for channel {ChannelId}", channelId);
                return Result.Fail(new Error("Failed to fetch posts").CausedBy(ex));
            }
        });

    internal Task<Result<ChannelModel>> FetchChannelInfo(ChannelId channelId) =>
        Task.Run(() =>
        {
            try
            {
                var feedUrl = $"{RssHubBaseUrl}/{channelId.ChannelName}";
                using var reader = XmlReader.Create(feedUrl);
                var feed = SyndicationFeed.Load(reader);

                var channelModel = new ChannelModel(
                    ChannelId: channelId,
                    Description: feed.Description.Text,
                    Name: feed.Title.Text,
                    ImageUrl: feed.ImageUrl ?? new Uri(feedUrl)
                );

                return Result.Ok(channelModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching channel info for {ChannelId}", channelId);
                return Result.Fail(new Error("Failed to fetch channel info").CausedBy(ex));
            }
        });

    private static string LinksCollectionToString(IEnumerable<SyndicationLink> links) =>
        string.Join(", ", links.Select(link => link.Uri.ToString()));
}
