using FluentResults;
using Microsoft.Extensions.Logging;

namespace TelegramDigest.Application.Services;

public class ChannelsService
{
    private readonly ChannelsRepository _channelsRepository;
    private readonly ChannelReader _channelReader;
    private readonly ILogger<ChannelsService> _logger;

    public ChannelsService(
        ChannelsRepository channelsRepository,
        ChannelReader channelReader,
        ILogger<ChannelsService> logger
    )
    {
        _channelsRepository = channelsRepository;
        _channelReader = channelReader;
        _logger = logger;
    }

    public async Task<Result> AddChannel(ChannelId channelId)
    {
        var channelResult = await _channelReader.FetchChannelInfo(channelId);
        if (channelResult.IsFailed)
        {
            return channelResult.ToResult();
        }

        return await _channelsRepository.SaveChannel(channelResult.Value);
    }

    public async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await _channelsRepository.LoadChannels();
    }

    public async Task<Result> RemoveChannel(ChannelId channelId)
    {
        var channels = await _channelsRepository.LoadChannels();
        if (channels.IsFailed)
        {
            return channels.ToResult();
        }

        if (!channels.Value.Any(c => c.ChannelId == channelId))
        {
            return Result.Fail(new Error($"Channel {channelId} not found"));
        }

        var updatedChannels = channels.Value
            .Where(c => c.ChannelId != channelId)
            .ToList();

        foreach (var channel in updatedChannels)
        {
            var result = await _channelsRepository.SaveChannel(channel);
            if (result.IsFailed)
            {
                return result;
            }
        }

        return Result.Ok();
    }
}
