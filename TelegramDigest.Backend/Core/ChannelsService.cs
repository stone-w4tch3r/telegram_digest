using FluentResults;

namespace TelegramDigest.Backend.Core;

internal interface IChannelsService
{
    /// <summary>
    /// Adds or updates a channel
    /// </summary>
    public Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId);

    /// <summary>
    /// Returns all non-deleted channels
    /// </summary>
    public Task<Result<List<ChannelModel>>> GetChannels();

    /// <summary>
    /// Marks a channel as deleted (soft delete)
    /// </summary>
    public Task<Result> RemoveChannel(ChannelTgId channelTgId);
}

internal sealed class ChannelsService(
    IChannelsRepository channelsRepository,
    IChannelReader channelReader,
    ILogger<ChannelsService> logger
) : IChannelsService
{
    private readonly ILogger<ChannelsService> _logger = logger;

    public async Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId)
    {
        var channelResult = await channelReader.FetchChannelInfo(channelTgId);
        if (channelResult.IsFailed)
        {
            return Result.Fail(channelResult.Errors);
        }

        return await channelsRepository.SaveChannel(channelResult.Value);
    }

    public async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await channelsRepository.LoadChannels();
    }

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId)
    {
        return await channelsRepository.DeleteChannel(channelTgId);
    }
}
