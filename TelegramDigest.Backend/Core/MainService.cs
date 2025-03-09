using System.ServiceModel.Syndication;
using FluentResults;

namespace TelegramDigest.Backend.Core;

public interface IMainService
{
    /// <summary>
    /// Generates daily digest for the last period, based on settings
    /// </summary>
    public Task<Result<DigestGenerationStatusModel>> ProcessDigestForLastPeriod(DigestId digestId);

    /// <summary>
    /// Queues the generation of a digest for the last period
    /// </summary>
    public Task<Result> QueueDigestForLastPeriod(DigestId digestId);

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

    /// <summary>
    /// Deletes a digest and all its posts
    /// </summary>
    public Task<Result> DeleteDigest(DigestId digestId);

    /// <summary>
    /// Send digest over email
    /// </summary>
    public Task<Result> SendDigestOverEmail(DigestId digestId);

    public Task<Result<SettingsModel>> GetSettings();
    public Task<Result> UpdateSettings(SettingsModel settings);
    public Task<Result<SyndicationFeed>> GetRssFeed();
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    IDigestService digestService,
    IChannelsService channelsService,
    IEmailSender emailSender,
    ISettingsManager settingsManager,
    IRssService rssService,
    ITaskQueue taskQueue,
    ILogger<MainService> logger
) : IMainService
{
    public async Task<Result<DigestGenerationStatusModel>> ProcessDigestForLastPeriod(
        DigestId digestId
    )
    {
        logger.LogDebug("Start processing digest for last period");

        var settings = await settingsManager.LoadSettings();
        if (settings.IsFailed)
        {
            return Result.Fail(settings.Errors);
        }

        //TODO handle 00:00
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var generationResult = await digestService.GenerateDigest(digestId, dateFrom, dateTo);
        if (generationResult.IsFailed)
        {
            logger.LogError(
                "Failed to generate digest: {Errors}",
                string.Join(", ", generationResult.Errors)
            );
            return Result.Fail(generationResult.Errors);
        }

        logger.LogInformation("Digest generation completed successfully");
        return Result.Ok(generationResult.Value);
    }

    [Obsolete("Will be removed before release")]
    public async Task<Result> SendDigestOverEmail(DigestId digestId)
    {
        var digestResult = await digestService.GetDigest(digestId);
        if (digestResult.IsFailed)
        {
            return Result.Fail(digestResult.Errors);
        }
        if (digestResult.Value is null)
        {
            return Result.Fail(new Error($"Failed to load digest {digestId} for email"));
        }

        var sendResult = await emailSender.SendDigest(digestResult.Value.DigestSummary);
        return sendResult.IsFailed ? Result.Fail(sendResult.Errors) : Result.Ok();
    }

    public Task<Result> QueueDigestForLastPeriod(DigestId digestId)
    {
        logger.LogDebug("Queueing digest generation for last period");
        taskQueue.QueueTask(async ct =>
        {
            logger.LogDebug("Start processing digest for last period from queue");
            await ProcessDigestForLastPeriod(digestId);
            logger.LogDebug("Finished processing digest for last period from queue");
        });

        logger.LogDebug("Queued digest generation successfully");
        return Task.FromResult(Result.Ok());
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
        return await digestService.GetDigestSummaries();
    }

    public async Task<Result<DigestModel?>> GetDigest(DigestId digestId)
    {
        return await digestService.GetDigest(digestId);
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
        return await digestService.DeleteDigest(digestId);
    }
}
