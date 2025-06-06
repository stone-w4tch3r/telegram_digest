using FluentResults;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IFeedsService
{
    /// <summary>
    /// Adds or updates a channel
    /// </summary>
    public Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId, CancellationToken ct);

    /// <summary>
    /// Returns all non-deleted channels
    /// </summary>
    public Task<Result<List<ChannelModel>>> GetChannels(CancellationToken ct);

    /// <summary>
    /// Marks a channel as deleted (soft delete)
    /// </summary>
    public Task<Result> RemoveChannel(ChannelTgId channelTgId, CancellationToken ct);
}

internal sealed class FeedsService(
    IFeedsRepository feedsRepository,
    IChannelReader channelReader,
    ILogger<FeedsService> logger
) : IFeedsService
{
    private readonly ILogger<FeedsService> _logger = logger;

    public async Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        var channelResult = await channelReader.FetchChannelInfo(channelTgId, ct);
        if (channelResult.IsFailed)
        {
            return Result.Fail(channelResult.Errors);
        }

        return await feedsRepository.SaveChannel(channelResult.Value, ct);
    }

    public async Task<Result<List<ChannelModel>>> GetChannels(CancellationToken ct)
    {
        return await feedsRepository.LoadChannels(ct);
    }

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        return await feedsRepository.DeleteChannel(channelTgId, ct);
    }
}
