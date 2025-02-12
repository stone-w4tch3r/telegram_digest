using FluentResults;
using TelegramDigest.Backend.Database;

namespace TelegramDigest.Backend.Core;

internal interface IChannelsRepository
{
    public Task<Result> SaveChannel(ChannelModel channel);
    public Task<Result<List<ChannelModel>>> LoadChannels();
    public Task<Result> DeleteChannel(ChannelTgId channelId);
}

internal sealed class ChannelsRepository(
    ApplicationDbContext dbContext,
    ILogger<ChannelsRepository> logger
) : IChannelsRepository
{
    public async Task<Result> SaveChannel(ChannelModel channel)
    {
        try
        {
            var entity = new ChannelEntity
            {
                TgId = channel.TgId.ChannelName,
                Title = channel.Title,
                Description = channel.Description,
                ImageUrl = channel.ImageUrl.ToString(),
            };

            var existing = await dbContext.Channels.FindAsync(entity.TgId);
            if (existing != null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await dbContext.Channels.AddAsync(entity);
            }

            await dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save channel [{ChannelId}]", channel.TgId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<ChannelModel>>> LoadChannels()
    {
        try
        {
            var entities = await dbContext.Channels.ToListAsync();
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

    public async Task<Result> DeleteChannel(ChannelTgId channelId)
    {
        try
        {
            var entity = await dbContext.Channels.FindAsync(channelId.ChannelName);
            if (entity == null)
            {
                return Result.Ok(); // Already deleted
            }

            dbContext.Channels.Remove(entity);
            await dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete channel [{ChannelId}]", channelId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }
}
