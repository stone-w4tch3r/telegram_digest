using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TelegramDigest.Application.Database;

namespace TelegramDigest.Application.Services;

public class ChannelsRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChannelsRepository> _logger;

    public ChannelsRepository(ApplicationDbContext dbContext, ILogger<ChannelsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Persists channel data, updating if exists or creating if new
    /// </summary>
    public async Task<Result> SaveChannel(ChannelModel channel)
    {
        try
        {
            var entity = new ChannelEntity
            {
                Id = channel.ChannelId.Value,
                Name = channel.Name,
                Description = channel.Description,
                ImageUrl = channel.ImageUrl.ToString(),
            };

            var existing = await _dbContext.Channels.FindAsync(entity.Id);
            if (existing != null)
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await _dbContext.Channels.AddAsync(entity);
            }

            await _dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save channel {ChannelId}", channel.ChannelId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<ChannelModel>>> LoadChannels()
    {
        try
        {
            var entities = await _dbContext.Channels.ToListAsync();
            var channels = entities
                .Select(e => new ChannelModel(
                    ChannelId: ChannelId.From(e.Id),
                    Name: e.Name,
                    Description: e.Description,
                    ImageUrl: new Uri(e.ImageUrl)
                ))
                .ToList();

            return Result.Ok(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load channels");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }
}
