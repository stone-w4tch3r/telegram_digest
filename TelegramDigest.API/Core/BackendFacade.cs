using FluentResults;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.API.Core;

/// <summary>
/// Facade for controllers
/// </summary>
public interface IApplicationFacade
{
    public Task<Result<List<ChannelDto>>> GetChannels();
    internal Task<Result> AddOrUpdateChannel(string channelName);
    internal Task<Result> RemoveChannel(string channelName);
    internal Task<Result<List<DigestSummaryDto>>> GetDigestSummaries();
    internal Task<Result<DigestDto?>> GetDigest(Guid digestId);
    internal Task<Result<DigestGenerationDto>> GenerateDigest();
    internal Task<Result<SettingsDto>> GetSettings();
    internal Task<Result> UpdateSettings(SettingsDto settingsDto);
}

/// <summary>
/// Facade for controllers
/// </summary>
internal sealed class BackendFacade(IMainService mainService, ILogger<BackendFacade> logger)
    : IApplicationFacade
{
    public async Task<Result<List<ChannelDto>>> GetChannels()
    {
        var result = await mainService.GetChannels();
        return result.Map(channels => channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result> AddOrUpdateChannel(string channelName)
    {
        var channelIdResult = ChannelTgId.TryFromString(channelName);
        if (channelIdResult.IsFailed)
        {
            logger.LogError("Failed to parse channel name: {ChannelName}", channelName);
            return Result.Fail(channelIdResult.Errors);
        }
        return await mainService.AddOrUpdateChannel(channelIdResult.Value);
    }

    public async Task<Result> RemoveChannel(string channelName)
    {
        var channelIdResult = ChannelTgId.TryFromString(channelName);
        if (channelIdResult.IsFailed)
        {
            logger.LogError("Failed to parse channel name: {ChannelName}", channelName);
            return Result.Fail(channelIdResult.Errors);
        }
        return await mainService.RemoveChannel(channelIdResult.Value);
    }

    public async Task<Result<List<DigestSummaryDto>>> GetDigestSummaries()
    {
        var result = await mainService.GetDigestSummaries();
        return result.Map(summaries => summaries.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<DigestDto?>> GetDigest(Guid digestId)
    {
        var result = await mainService.GetDigest(new(digestId));
        return result.Map(digest => digest?.ToDto());
    }

    public async Task<Result<DigestGenerationDto>> GenerateDigest()
    {
        var digestId = new DigestId();
        var digestResult = await mainService.ProcessDigestForLastPeriod(digestId);
        if (digestResult.IsFailed)
        {
            return Result.Fail(digestResult.Errors);
        }

        var summariesResult = await GetDigestSummaries();
        if (summariesResult.IsFailed)
        {
            return Result.Fail(summariesResult.Errors);
        }

        return Result.Ok<DigestGenerationDto>(
            digestResult.Value switch
            {
                DigestGenerationStatusModel.Success => new(
                    digestId.Guid,
                    DigestGenerationStatusDto.Success
                ),
                DigestGenerationStatusModel.NoPosts => new(null, DigestGenerationStatusDto.NoPosts),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(digestResult),
                    digestResult,
                    null
                ),
            }
        );
    }

    public async Task<Result<SettingsDto>> GetSettings()
    {
        var result = await mainService.GetSettings();
        return result.Map(settings => settings.ToDto());
    }

    public async Task<Result> UpdateSettings(SettingsDto settingsDto)
    {
        return await mainService.UpdateSettings(settingsDto.ToDomain());
    }
}
