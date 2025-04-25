using FluentResults;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Backend.Db;

internal interface IChannelsRepository
{
    // Obsolete channel methods
    Task<Result> SaveChannel(ChannelModel channel, CancellationToken cancellationToken);
    Task<Result<List<ChannelModel>>> LoadChannels(CancellationToken cancellationToken);
    Task<Result> DeleteChannel(string channelTgId, CancellationToken cancellationToken);

    // New feed methods
    Task<Result> SaveFeed(FeedModel feed, CancellationToken cancellationToken);
    Task<Result<List<FeedModel>>> LoadFeeds(CancellationToken cancellationToken);
    Task<Result> DeleteFeed(Uri feedUrl, CancellationToken cancellationToken);
}

// tmp models
public sealed record FeedModel(Uri RssUrl, string Description, string Title, Uri ImageUrl);

internal sealed class ChannelsRepository(
    ApplicationDbContext dbContext,
    ILogger<ChannelsRepository> logger
) : IChannelsRepository
{
    public Task<Result> SaveChannel(ChannelModel channel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Obsolete, delete this!");
    }

    // TODO
    public Task<Result<List<ChannelModel>>> LoadChannels(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Obsolete, delete this!");
    }

    // TODO
    public Task<Result> DeleteChannel(string channelTgId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Obsolete, delete this!");
    }

    public async Task<Result> SaveFeed(FeedModel feed, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new FeedEntity
            {
                RssUrl = feed.RssUrl.ToString(),
                Title = feed.Title,
                Description = feed.Description,
                ImageUrl = feed.ImageUrl.ToString(),
                IsDeleted = false,
            };

            var existing = await dbContext.Feeds.FindAsync([entity.RssUrl], cancellationToken);
            if (existing != null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await dbContext.Feeds.AddAsync(entity, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save feed [{FeedUrl}]", feed.RssUrl);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<FeedModel>>> LoadFeeds(CancellationToken cancellationToken)
    {
        try
        {
            var entities = await dbContext
                .Feeds.Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken);

            var feeds = entities
                .Select(e => new FeedModel(
                    RssUrl: new(e.RssUrl),
                    Title: e.Title,
                    Description: e.Description,
                    ImageUrl: new(e.ImageUrl)
                ))
                .ToList();

            return Result.Ok(feeds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load feeds");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteFeed(Uri feedUrl, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await dbContext.Feeds.FindAsync([feedUrl.ToString()], cancellationToken);
            if (entity == null)
            {
                return Result.Ok(); // Already deleted
            }

            entity.IsDeleted = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete feed [{FeedUrl}]", feedUrl);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }
}
