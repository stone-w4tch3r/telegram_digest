using FluentResults;

namespace TelegramDigest.Application.Services;

internal interface IChannelsService
{
    public Task<Result> AddChannel(ChannelTgId channelTgId);
    public Task<Result<List<ChannelModel>>> GetChannels();
    public Task<Result> RemoveChannel(ChannelTgId channelTgId);
}

internal sealed class ChannelsService(
    IChannelsRepository channelsRepository,
    IChannelReader channelReader,
    ILogger<ChannelsService> logger
) : IChannelsService
{
    private readonly ILogger<ChannelsService> _logger = logger;

    public async Task<Result> AddChannel(ChannelTgId channelTgId)
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
