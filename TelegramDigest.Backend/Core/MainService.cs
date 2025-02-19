using System.ServiceModel.Syndication;
using FluentResults;

namespace TelegramDigest.Backend.Core;

public interface IMainService
{
    /// <summary>
    /// Generates and sends daily digest according to configured schedule
    /// </summary>
    public Task<Result<DigestId?>> ProcessDailyDigest();

    /// <summary>
    /// Returns all non-deleted channels
    /// </summary>
    public Task<Result<List<ChannelModel>>> GetChannels();

    /// <summary>
    /// Adds or updates a channel
    /// </summary>
    public Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId);

    /// <summary>
    /// Marks a channel as deleted (soft delete)
    /// </summary>
    public Task<Result> RemoveChannel(ChannelTgId channelTgId);

    /// <summary>
    /// Loads all digest summaries (metadata for each digest) without posts
    /// </summary>
    public Task<Result<DigestSummaryModel[]>> GetDigestSummaries();

    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel?>> GetDigest(DigestId digestId);
    public Task<Result<SettingsModel>> GetSettings();
    public Task<Result> UpdateSettings(SettingsModel settings);
    public Task<Result<SyndicationFeed>> GetRssFeed();
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    IDigestsService digestsService,
    IChannelsService channelsService,
    IEmailSender emailSender,
    ISettingsManager settingsManager,
    IRssService rssService,
    ILogger<MainService> logger
) : IMainService
{
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
        if (digestResult.Value is null)
        {
            return Result.Fail(new Error($"Failed to load created digest {digestId}"));
        }

        var sendResult = await emailSender.SendDigest(digestResult.Value.DigestSummary);
        return sendResult.IsFailed
            ? Result.Fail(sendResult.Errors)
            : Result.Ok((DigestId?)digestId);
    }

    public async Task<Result<List<ChannelModel>>> GetChannels()
    {
        return await channelsService.GetChannels();
    }

    public async Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId)
    {
        return await channelsService.AddOrUpdateChannel(channelTgId);
    }

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId)
    {
        return await channelsService.RemoveChannel(channelTgId);
    }

    public async Task<Result<DigestSummaryModel[]>> GetDigestSummaries()
    {
        return await digestsService.GetDigestSummaries();
    }

    public async Task<Result<DigestModel?>> GetDigest(DigestId digestId)
    {
        return await digestsService.GetDigest(digestId);
    }

    public async Task<Result<SettingsModel>> GetSettings()
    {
        return await settingsManager.LoadSettings();
    }

    public async Task<Result> UpdateSettings(SettingsModel settings)
    {
        return await settingsManager.SaveSettings(settings);
    }

    public async Task<Result<SyndicationFeed>> GetRssFeed()
    {
        return await rssService.GenerateRssFeed();
    }

    public async Task<Result> DeleteDigest(DigestId digestId)
    {
        return await digestsService.DeleteDigest(digestId);
    }
}
