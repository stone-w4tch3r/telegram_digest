using System.ServiceModel.Syndication;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramDigest.Backend.Core;

public interface IMainService
{
    /// <summary>
    /// Generates daily digest for the last period, based on settings
    /// </summary>
    public Task<Result<DigestGenerationResultTypeModelEnum>> ProcessDigestForLastPeriod(
        DigestId digestId
    );

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
    /// Returns history of digest statuses
    /// </summary>
    public Task<Result<IDigestStepModel[]>> GetDigestSteps(DigestId digestId);

    /// <summary>
    /// Tries to cancel a digest
    /// </summary>
    /// <returns>true if success, false if not found</returns>
    public Result TryCancelDigest(DigestId digestId);

    /// <summary>
    /// Returns list of digests that are currently being processed
    /// </summary>
    public IReadOnlyCollection<DigestId> GetInProgressDigests();

    /// <summary>
    /// Returns list of digests that are waiting to be processed
    /// </summary>
    public IReadOnlyCollection<DigestId> GetWaitingDigests();

    /// <summary>
    /// Tries to remove a digest from the waiting list
    /// </summary>
    /// <returns>true if success, false if not found</returns>
    public Result TryRemoveWaitingDigest(DigestId key);

    /// <summary>
    /// Send digest over email
    /// </summary>
    public Task<Result> SendDigestOverEmail(DigestId digestId);

    /// <summary>
    /// Loads user settings
    /// </summary>
    public Task<Result<SettingsModel>> GetSettings();

    /// <summary>
    /// Saves user settings
    /// </summary>
    public Task<Result> UpdateSettings(SettingsModel settings);

    /// <summary>
    /// Returns list of digests as RSS feed
    /// </summary>
    public Task<Result<SyndicationFeed>> GetRssFeed();
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    IDigestService digestService,
    IChannelsService channelsService,
    IEmailSender emailSender,
    IServiceProvider serviceProvider,
    ISettingsManager settingsManager,
    IRssService rssService,
    ITaskScheduler<DigestId> taskTracker,
    IDigestStepsService digestStepsService,
    ILogger<MainService> logger
) : IMainService
{
    public async Task<Result<DigestGenerationResultTypeModelEnum>> ProcessDigestForLastPeriod(
        DigestId digestId
    )
    {
        await digestStepsService.AddStep(
            new SimpleStepModel
            {
                DigestId = digestId,
                Type = DigestStepTypeModelEnum.ProcessingStarted,
            }
        );

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

    public async Task<Result> QueueDigestForLastPeriod(DigestId digestId)
    {
        await digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Queued }
        );

        taskTracker.AddTaskToWaitQueue(
            async ct => // TODO pass ct
            {
                // use own scope and services to avoid issues with disposing of captured scope
                using var scope = serviceProvider.CreateScope();
                var scopedMainService = scope.ServiceProvider.GetRequiredService<IMainService>();
                await scopedMainService.ProcessDigestForLastPeriod(digestId);
            },
            digestId
        );

        return Result.Ok();
    }

    public Result TryCancelDigest(DigestId digestId)
    {
        return Result.Try(() => taskTracker.CancelTaskInProgress(digestId));
    }

    public IReadOnlyCollection<DigestId> GetInProgressDigests()
    {
        return taskTracker.GetInProgressTasks();
    }

    public IReadOnlyCollection<DigestId> GetWaitingDigests()
    {
        return taskTracker.GetWaitingTasks();
    }

    public Result TryRemoveWaitingDigest(DigestId key)
    {
        return Result.Try(() => taskTracker.RemoveWaitingTask(key));
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

    public async Task<Result<IDigestStepModel[]>> GetDigestSteps(DigestId digestId)
    {
        return await digestStepsService.GetAllSteps(digestId);
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
}
