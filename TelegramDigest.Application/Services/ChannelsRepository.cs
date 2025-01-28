using FluentResults;
using TelegramDigest.Application.Database;

namespace TelegramDigest.Application.Services;

internal sealed class ChannelsRepository(
    ApplicationDbContext dbContext,
    ILogger<ChannelsRepository> logger
)
{
    /// <summary>
    /// Persists channel data, updating if exists or creating if new
    /// </summary>
    internal async Task<Result> SaveChannel(ChannelModel channel)
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

    internal async Task<Result<List<ChannelModel>>> LoadChannels()
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
}
