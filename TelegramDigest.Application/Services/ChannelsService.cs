using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class ChannelsService(
    ChannelsRepository channelsRepository,
    ChannelReader channelReader,
    ILogger<ChannelsService> logger
)
{
    private readonly ILogger<ChannelsService> _logger = logger;

    internal async Task<Result> AddChannel(ChannelTgId channelTgId)
    {
        var channelResult = await channelReader.FetchChannelInfo(channelTgId);
        if (channelResult.IsFailed)
        {
            return Result.Fail(channelResult.Errors);
        }

        return await channelsRepository.SaveChannel(channelResult.Value);
    }

    internal async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await channelsRepository.LoadChannels();
    }

    internal async Task<Result> RemoveChannel(ChannelTgId channelTgId)
    {
        return await channelsRepository.DeleteChannel(channelTgId);
    }
}
