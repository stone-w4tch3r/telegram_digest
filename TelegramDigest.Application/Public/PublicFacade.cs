using FluentResults;
using TelegramDigest.Application.Services;

namespace TelegramDigest.Application.Public;

/// <summary>
/// Public API facade for the application, used by Web UI and other external consumers
/// </summary>
public sealed class PublicFacade(IMainService mainService, ILogger<PublicFacade> logger)
{
    public async Task<Result<List<ChannelDto>>> GetChannels()
    {
        var result = await mainService.GetChannels();
        return result.Map(channels => channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result> AddChannel(string channelName)
    {
        return await mainService.AddChannel(new(channelName));
    }

    public async Task<Result> RemoveChannel(string channelName)
    {
        return await mainService.RemoveChannel(new(channelName));
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
