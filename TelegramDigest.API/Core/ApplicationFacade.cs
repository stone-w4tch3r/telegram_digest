using FluentResults;
using TelegramDigest.Application.Services;

namespace TelegramDigest.API.Core;

/// <summary>
/// Facade for controllers
/// </summary>
public interface IApplicationFacade
{
    public Task<Result<List<ChannelDto>>> GetChannels();
    internal Task<Result> AddChannel(string channelName);
    internal Task<Result> RemoveChannel(string channelName);
    internal Task<Result<List<DigestSummaryDto>>> GetDigestSummaries();
    internal Task<Result<DigestDto>> GetDigest(Guid digestId);
    internal Task<Result<DigestGenerationDto>> GenerateDigest();
    internal Task<Result<SettingsDto>> GetSettings();
    internal Task<Result> UpdateSettings(SettingsDto settingsDto);
}

/// <summary>
/// Facade for controllers
/// </summary>
internal sealed class ApplicationFacade(IMainService mainService, ILogger<ApplicationFacade> logger)
    : IApplicationFacade
{
    public async Task<Result<List<ChannelDto>>> GetChannels()
    {
        var result = await mainService.GetChannels();
        return result.Map(channels => channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result> AddChannel(string channelName)
    {
        var channelIdResult = ChannelTgId.TryFromString(channelName);
        if (channelIdResult.IsFailed)
        {
            logger.LogError("Failed to parse channel name: {ChannelName}", channelName);
            return Result.Fail(channelIdResult.Errors);
        }
        return await mainService.AddChannel(channelIdResult.Value);
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

    public async Task<Result<DigestDto>> GetDigest(Guid digestId)
    {
        var result = await mainService.GetDigest(new(digestId));
        return result.Map(digest => digest.ToDto());
    }

    public async Task<Result<DigestGenerationDto>> GenerateDigest()
    {
        var digestResult = await mainService.ProcessDailyDigest();
        if (digestResult.IsFailed)
        {
            return Result.Fail(digestResult.Errors);
        }

        var summariesResult = await GetDigestSummaries();
        return summariesResult.IsFailed
            ? Result.Fail(summariesResult.Errors)
            : Result.Ok<DigestGenerationDto>(
                digestResult.Value?.Id is { } id
                    ? new(id, DigestGenerationStatus.Success)
                    : new(null, DigestGenerationStatus.NoPosts)
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
