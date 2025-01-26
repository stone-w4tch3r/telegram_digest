using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class ChannelReader(ILogger<ChannelReader> logger)
{
    private const string RssHubBaseUrl = "https://rsshub.app/telegram/channel";

    internal Task<Result<List<PostModel>>> FetchPosts(
        ChannelId channelId,
        DateTime from,
        DateTime to
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
                        x.PublishDate.DateTime >= from && x.PublishDate.DateTime <= to
                    )
                    .Select(x =>
                    {
                        var url =
                            x.Links.SingleOrDefault()?.Uri
                            ?? throw new FormatException(
                                $"Telegram Channel RSS item does not have a valid URL"
                            );

                        return new PostModel(
                            ChannelId: channelId,
                            Title: x.Title.Text,
                            Content: x.Summary.Text,
                            Url: url,
                            PublishedAt: x.PublishDate.DateTime
                        );
                    })
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
}
