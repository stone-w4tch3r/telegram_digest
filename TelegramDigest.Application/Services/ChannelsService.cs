using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class ChannelsService(
    ChannelsRepository channelsRepository,
    ChannelReader channelReader,
    ILogger<ChannelsService> logger
)
{
    private readonly ILogger<ChannelsService> _logger = logger;

    internal async Task<Result> AddChannel(ChannelId channelId)
    {
        var channelResult = await channelReader.FetchChannelInfo(channelId);
        if (channelResult.IsFailed)
        {
            return channelResult.ToResult();
        }

        return await channelsRepository.SaveChannel(channelResult.Value);
    }

    internal async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await channelsRepository.LoadChannels();
    }

    internal async Task<Result> RemoveChannel(ChannelId channelId)
    {
        var channels = await channelsRepository.LoadChannels();
        if (channels.IsFailed)
        {
            return channels.ToResult();
        }

        if (!channels.Value.Any(c => c.ChannelId == channelId))
        {
            return Result.Fail(new Error($"Channel {channelId} not found"));
        }

        var updatedChannels = channels.Value.Where(c => c.ChannelId != channelId).ToList();

        foreach (var channel in updatedChannels)
        {
            var result = await channelsRepository.SaveChannel(channel);
            if (result.IsFailed)
            {
                return result;
            }
        }

        return Result.Ok();
    }
}
