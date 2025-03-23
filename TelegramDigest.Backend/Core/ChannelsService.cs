using FluentResults;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IChannelsService
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

internal sealed class ChannelsService(
    IChannelsRepository channelsRepository,
    IChannelReader channelReader,
    ILogger<ChannelsService> logger
) : IChannelsService
{
    private readonly ILogger<ChannelsService> _logger = logger;

    public async Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        var channelResult = await channelReader.FetchChannelInfo(channelTgId, ct);
        if (channelResult.IsFailed)
        {
            return Result.Fail(channelResult.Errors);
        }

        return await channelsRepository.SaveChannel(channelResult.Value, ct);
    }

    public async Task<Result<List<ChannelModel>>> GetChannels(CancellationToken ct)
    {
        return await channelsRepository.LoadChannels(ct);
    }

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        return await channelsRepository.DeleteChannel(channelTgId, ct);
    }
}
