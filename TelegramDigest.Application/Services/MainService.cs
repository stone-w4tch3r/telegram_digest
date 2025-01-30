using FluentResults;

namespace TelegramDigest.Application.Services;

// ReSharper disable once InconsistentNaming
public abstract class IMainService
{
    internal abstract Task<Result<DigestId?>> ProcessDailyDigest();
    internal abstract Task<Result<List<ChannelModel>>> GetChannels();
    internal abstract Task<Result> AddChannel(ChannelTgId channelTgId);
    internal abstract Task<Result> RemoveChannel(ChannelTgId channelTgId);
    internal abstract Task<Result<List<DigestSummaryModel>>> GetDigestSummaries();
    internal abstract Task<Result<DigestModel>> GetDigest(DigestId digestId);
    internal abstract Task<Result<SettingsModel>> GetSettings();
    internal abstract Task<Result> UpdateSettings(SettingsModel settings);
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    DigestsService digestsService,
    ChannelsService channelsService,
    EmailSender emailSender,
    SettingsManager settingsManager,
    ILogger<MainService> logger
) : IMainService
{
    /// <summary>
    /// Generates and sends daily digest according to configured schedule
    /// </summary>
    internal override async Task<Result<DigestId?>> ProcessDailyDigest()
    {
        logger.LogInformation("Starting daily digest processing");

        var settings = await settingsManager.LoadSettings();
        if (settings.IsFailed)
        {
            return Result.Fail(settings.Errors);
        }

        //TODO handle 00:00
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var generationResult = await digestsService.GenerateDigest(dateFrom, dateTo);
        if (generationResult.IsFailed)
        {
            return Result.Fail(generationResult.Errors);
        }
        if (generationResult.Value is null)
        {
            return Result.Ok((DigestId?)null);
        }

        var digestId = generationResult.Value.Value;
        var digestResult = await digestsService.GetDigest(digestId);
        if (digestResult.IsFailed)
        {
            return Result.Fail(digestResult.Errors);
        }

        var sendResult = await emailSender.SendDigest(digestResult.Value.DigestSummary);
        return sendResult.IsFailed
            ? Result.Fail(sendResult.Errors)
            : Result.Ok((DigestId?)digestId);
    }

    internal override async Task<Result<List<ChannelModel>>> GetChannels() =>
        await channelsService.GetChannels();

    internal override async Task<Result> AddChannel(ChannelTgId channelTgId) =>
        await channelsService.AddChannel(channelTgId);

    internal override async Task<Result> RemoveChannel(ChannelTgId channelTgId) =>
        await channelsService.RemoveChannel(channelTgId);

    internal override async Task<Result<List<DigestSummaryModel>>> GetDigestSummaries() =>
        await digestsService.GetDigestSummaries();

    internal override async Task<Result<DigestModel>> GetDigest(DigestId digestId) =>
        await digestsService.GetDigest(digestId);

    internal override async Task<Result<SettingsModel>> GetSettings() =>
        await settingsManager.LoadSettings();

    internal override async Task<Result> UpdateSettings(SettingsModel settings) =>
        await settingsManager.SaveSettings(settings);
}
