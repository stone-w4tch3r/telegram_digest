using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;

namespace TelegramDigest.Application.Services;

public class ChannelReader
{
    private readonly ILogger<ChannelReader> _logger;
    private const string RssHubBaseUrl = "https://rsshub.app/telegram/channel/";

    public ChannelReader(ILogger<ChannelReader> logger)
    {
        _logger = logger;
    }

    public async Task<Result<List<PostModel>>> FetchPosts(ChannelId channelId)
    {
        try
        {
            var feedUrl = $"{RssHubBaseUrl}{channelId.Value}";
            using var reader = XmlReader.Create(feedUrl);
            var feed = SyndicationFeed.Load(reader);

            var posts = feed
                .Items.Select(item => new PostModel(
                    PostId: PostId.NewId(),
                    ChannelId: channelId,
                    Title: item.Title.Text,
                    Description: item.Summary.Text,
                    Url: item.Links.FirstOrDefault()?.Uri ?? new Uri(feedUrl),
                    PublishedAt: item.PublishDate.DateTime
                ))
                .ToList();

            return Result.Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching posts for channel {ChannelId}", channelId);
            return Result.Fail(new Error("Failed to fetch posts").CausedBy(ex));
        }
    }

    public async Task<Result<ChannelModel>> FetchChannelInfo(ChannelId channelId)
    {
        try
        {
            var feedUrl = $"{RssHubBaseUrl}{channelId.Value}";
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
            _logger.LogError(ex, "Error fetching channel info for {ChannelId}", channelId);
            return Result.Fail(new Error("Failed to fetch channel info").CausedBy(ex));
        }
    }
}
