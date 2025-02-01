using FluentResults;

namespace TelegramDigest.Application.Core;

public interface IMainService
{
    public Task<Result<DigestId?>> ProcessDailyDigest();
    public Task<Result<List<ChannelModel>>> GetChannels();
    public Task<Result> AddChannel(ChannelTgId channelTgId);
    public Task<Result> RemoveChannel(ChannelTgId channelTgId);
    public Task<Result<List<DigestSummaryModel>>> GetDigestSummaries();
    public Task<Result<DigestModel>> GetDigest(DigestId digestId);
    public Task<Result<SettingsModel>> GetSettings();
    public Task<Result> UpdateSettings(SettingsModel settings);
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    IDigestsService digestsService,
    IChannelsService channelsService,
    IEmailSender emailSender,
    ISettingsManager settingsManager,
    ILogger<MainService> logger
) : IMainService
{
    /// <summary>
    /// Generates and sends daily digest according to configured schedule
    /// </summary>
    public async Task<Result<DigestId?>> ProcessDailyDigest()
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

    public async Task<Result<List<ChannelModel>>> GetChannels() =>
        await channelsService.GetChannels();

    public async Task<Result> AddChannel(ChannelTgId channelTgId) =>
        await channelsService.AddChannel(channelTgId);

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId) =>
        await channelsService.RemoveChannel(channelTgId);

    public async Task<Result<List<DigestSummaryModel>>> GetDigestSummaries() =>
        await digestsService.GetDigestSummaries();

    public async Task<Result<DigestModel>> GetDigest(DigestId digestId) =>
        await digestsService.GetDigest(digestId);

    public async Task<Result<SettingsModel>> GetSettings() => await settingsManager.LoadSettings();

    public async Task<Result> UpdateSettings(SettingsModel settings) =>
        await settingsManager.SaveSettings(settings);
}
