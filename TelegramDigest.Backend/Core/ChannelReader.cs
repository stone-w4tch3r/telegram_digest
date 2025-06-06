using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend.DeploymentOptions;

namespace TelegramDigest.Backend.Core;

internal interface IFeedReader
{
    Task<Result<List<PostModel>>> FetchPosts(
        FeedUrl feedUrl,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );
    Task<Result<FeedModel>> FetchFeedInfo(FeedUrl feedUrl, CancellationToken ct);
}

internal sealed class FeedReader(ILogger<FeedReader> logger) : IFeedReader
{
    public Task<Result<List<PostModel>>> FetchPosts(
        FeedUrl feedUrl,
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
                    using var reader = XmlReader.Create(feedUrl.Url.ToString());
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var posts = feed
                        .Items.Where(x =>
                            DateOnly.FromDateTime(x.PublishDate.DateTime) >= from
                            && DateOnly.FromDateTime(x.PublishDate.DateTime) <= to
                        )
                        .Select(x => new PostModel(
                            FeedUrl: feedUrl,
                            HtmlContent: new(x.Summary.Text),
                            Url: x.Links.SingleOrDefault()?.Uri
                                ?? throw new FormatException(
                                    $"Feed item [{x.Id}] does not have a valid URL [{LinksCollectionToString(x.Links)}]"
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
                    logger.LogError(ex, "Error fetching posts for feed {FeedUrl}", feedUrl);
                    return Result.Fail(
                        new Error($"Error fetching posts for feed {feedUrl}").CausedBy(ex)
                    );
                }
            },
            ct
        );

    public Task<Result<FeedModel>> FetchFeedInfo(FeedUrl feedUrl, CancellationToken ct) =>
        Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var reader = XmlReader.Create(feedUrl.Url.ToString());
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var feedModel = new FeedModel(
                        FeedUrl: feedUrl,
                        Description: feed.Description.Text,
                        Title: feed.Title.Text,
                        ImageUrl: feed.ImageUrl ?? new Uri(feedUrl.Url.ToString())
                    );

                    return Result.Ok(feedModel);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching feed info for {FeedUrl}", feedUrl);
                    return Result.Fail(new Error("Failed to fetch feed info").CausedBy(ex));
                }
            },
            ct
        );

    private static string LinksCollectionToString(IEnumerable<SyndicationLink> links) =>
        string.Join(", ", links.Select(link => link.Uri.ToString()));
}
