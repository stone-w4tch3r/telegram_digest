using FluentResults;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Backend.Db;

internal interface IChannelsRepository
{
    /// <summary>
    /// Saves or updates a channel in the repository
    /// </summary>
    public Task<Result> SaveChannel(ChannelModel channel, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all non-deleted channels from the repository
    /// </summary>
    public Task<Result<List<ChannelModel>>> LoadChannels(CancellationToken cancellationToken);

    /// <summary>
    /// Marks a channel as deleted in the repository (soft delete)
    /// </summary>
    public Task<Result> DeleteChannel(ChannelTgId channelId, CancellationToken cancellationToken);
}

internal sealed class ChannelsRepository(
    ApplicationDbContext dbContext,
    ILogger<ChannelsRepository> logger
) : IChannelsRepository
{
    public async Task<Result> SaveChannel(ChannelModel channel, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new ChannelEntity
            {
                TgId = channel.TgId.ChannelName,
                Title = channel.Title,
                Description = channel.Description,
                ImageUrl = channel.ImageUrl.ToString(),
                IsDeleted = false,
            };

            var existing = await dbContext.Channels.FindAsync([entity.TgId], cancellationToken);
            if (existing != null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await dbContext.Channels.AddAsync(entity, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save channel [{ChannelId}]", channel.TgId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<ChannelModel>>> LoadChannels(CancellationToken cancellationToken)
    {
        try
        {
            var entities = await dbContext
                .Channels.Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken);
            var channels = entities
                .Select(e => new ChannelModel(
                    TgId: new(e.TgId),
                    Title: e.Title,
                    Description: e.Description,
                    ImageUrl: new(e.ImageUrl)
                ))
                .ToList();

            return Result.Ok(channels);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load channels");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteChannel(
        ChannelTgId channelId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var entity = await dbContext.Channels.FindAsync(
                [channelId.ChannelName],
                cancellationToken
            );
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
            logger.LogError(ex, "Failed to delete channel [{ChannelId}]", channelId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }
}
