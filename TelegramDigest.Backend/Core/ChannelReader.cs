using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Backend.Core;

internal interface IChannelReader
{
    Task<Result<List<PostModel>>> FetchPosts(
        ChannelTgId channelTgId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );
    Task<Result<ChannelModel>> FetchChannelInfo(ChannelTgId channelTgId, CancellationToken ct);
}

internal sealed class ChannelReader(
    ILogger<ChannelReader> logger,
    IOptions<BackendDeploymentOptions> options
) : IChannelReader
{
    private readonly Uri _telegramRssBaseUrl = options.Value.TelegramRssBaseUrl;

    public Task<Result<List<PostModel>>> FetchPosts(
        ChannelTgId channelTgId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    ) =>
        Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();

                if (from > to)
                {
                    return Result.Fail(
                        $"Can't fetch posts, invalid period requested, from: [{from}] to [{to}]"
                    );
                }

                try
                {
                    ct.ThrowIfCancellationRequested();
                    var feedUrl = $"{_telegramRssBaseUrl}/{channelTgId.ChannelName}";
                    using var reader = XmlReader.Create(feedUrl);
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var posts = feed
                        .Items.Where(x =>
                            DateOnly.FromDateTime(x.PublishDate.DateTime) >= from
                            && DateOnly.FromDateTime(x.PublishDate.DateTime) <= to
                        )
                        .Select(x => new PostModel(
                            ChannelTgId: channelTgId,
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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error fetching posts for channel {ChannelId}",
                        channelTgId
                    );
                    return Result.Fail(
                        new Error($"Error fetching posts for channel {channelTgId}").CausedBy(ex)
                    );
                }
            },
            ct
        );

    public Task<Result<ChannelModel>> FetchChannelInfo(
        ChannelTgId channelTgId,
        CancellationToken ct
    ) =>
        Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var feedUrl = $"{_telegramRssBaseUrl}/{channelTgId.ChannelName}";
                    using var reader = XmlReader.Create(feedUrl);
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var channelModel = new ChannelModel(
                        TgId: channelTgId,
                        Description: feed.Description.Text,
                        Title: feed.Title.Text,
                        ImageUrl: feed.ImageUrl ?? new Uri(feedUrl)
                    );

                    return Result.Ok(channelModel);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching channel info for {ChannelId}", channelTgId);
                    return Result.Fail(new Error("Failed to fetch channel info").CausedBy(ex));
                }
            },
            ct
        );

    private static string LinksCollectionToString(IEnumerable<SyndicationLink> links) =>
        string.Join(", ", links.Select(link => link.Uri.ToString()));
}
