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
            return channelResult.ToResult();
        }

        return await channelsRepository.SaveChannel(channelResult.Value);
    }

    internal async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await channelsRepository.LoadChannels();
    }

    internal async Task<Result> RemoveChannel(ChannelTgId channelTgId)
    {
        var channelsResult = await channelsRepository.LoadChannels();
        if (channelsResult.IsFailed)
        {
            return channelsResult.ToResult();
        }

        if (channelsResult.Value.All(c => c.TgId != channelTgId))
        {
            return Result.Fail(new Error($"Channel [{channelTgId}] not found"));
        }

        var updatedChannels = channelsResult.Value.Where(c => c.TgId != channelTgId).ToList();

        // TODO foooooooooooo
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
