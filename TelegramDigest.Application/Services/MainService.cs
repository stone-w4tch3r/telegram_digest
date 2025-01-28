using FluentResults;

namespace TelegramDigest.Application.Services;

// ReSharper disable once InconsistentNaming
public abstract class IMainService
{
    internal abstract Task<Result> ProcessDailyDigest();
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
    internal override async Task<Result> ProcessDailyDigest()
    {
        logger.LogInformation("Starting daily digest processing");

        var settings = await settingsManager.LoadSettings();
        if (settings.IsFailed)
        {
            return settings.ToResult();
        }

        //TODO handle 00:00
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var generationResult = await digestsService.GenerateDigest(dateFrom, dateTo);
        if (generationResult.IsFailed)
        {
            return generationResult.ToResult();
        }

        var digest = await digestsService.GetDigest(generationResult.Value);
        if (digest.IsFailed)
        {
            return digest.ToResult();
        }

        return await emailSender.SendDigest(digest.Value.DigestSummary);
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
