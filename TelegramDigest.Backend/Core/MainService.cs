using System.ServiceModel.Syndication;
using FluentResults;

namespace TelegramDigest.Backend.Core;

public interface IMainService
{
    /// <summary>
    /// Generates digest based on provided filter parameters
    /// </summary>
    Task<Result<DigestGenerationResultModelEnum>> ProcessDigest(
        DigestId digestId,
        DigestFilterModel filter,
        CancellationToken ct
    );

    /// <summary>
    /// Queues the generation of a digest with specified filter parameters
    /// </summary>
    Task<Result> QueueDigest(DigestId digestId, DigestFilterModel filter, CancellationToken ct);

    /// <summary>
    /// Returns all non-deleted channels
    /// </summary>
    Task<Result<List<ChannelModel>>> GetChannels(CancellationToken ct);

    /// <summary>
    /// Adds or updates a channel
    /// </summary>
    Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId, CancellationToken ct);

    /// <summary>
    /// Marks a channel as deleted (soft delete)
    /// </summary>
    Task<Result> RemoveChannel(ChannelTgId channelTgId, CancellationToken ct);

    /// <summary>
    /// Loads all digest summaries (metadata for each digest) without posts
    /// </summary>
    Task<Result<DigestSummaryModel[]>> GetDigestSummaries(CancellationToken ct);

    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    Task<Result<DigestModel?>> GetDigest(DigestId digestId, CancellationToken ct);

    /// <summary>
    /// Deletes a digest and all its posts
    /// </summary>
    Task<Result> DeleteDigest(DigestId digestId, CancellationToken ct);

    /// <summary>
    /// Returns history of digest statuses
    /// </summary>
    Task<Result<IDigestStepModel[]>> GetDigestSteps(DigestId digestId, CancellationToken ct);

    /// <summary>
    /// Tries to cancel a digest
    /// </summary>
    /// <returns>true if success, false if not found</returns>
    Task<Result> CancelDigest(DigestId digestId);

    /// <summary>
    /// Returns list of digests that are currently being processed
    /// </summary>
    Task<DigestId[]> GetInProgressDigests();

    /// <summary>
    /// Returns list of digests that are waiting to be processed
    /// </summary>
    Task<DigestId[]> GetWaitingDigests();

    /// <summary>
    /// Returns list of digests that were requested to cancel, but not canceled yet
    /// </summary>
    Task<DigestId[]> GetCancellationRequestedDigests();

    /// <summary>
    /// Tries to remove a digest from the waiting list
    /// </summary>
    /// <returns>true if success, false if not found</returns>
    Task<Result> RemoveWaitingDigest(DigestId key);

    /// <summary>
    /// Send digest over email
    /// </summary>
    Task<Result> SendDigestOverEmail(DigestId digestId, CancellationToken ct);

    /// <summary>
    /// Loads user settings
    /// </summary>
    Task<Result<SettingsModel>> GetSettings(CancellationToken ct);

    /// <summary>
    /// Saves user settings
    /// </summary>
    Task<Result> UpdateSettings(SettingsModel settings, CancellationToken ct);

    /// <summary>
    /// Returns list of digests as RSS feed
    /// </summary>
    Task<Result<SyndicationFeed>> GetRssFeed(CancellationToken ct);
}

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
internal sealed class MainService(
    IDigestService digestService,
    IChannelsService channelsService,
    IEmailSender emailSender,
    IDigestProcessingOrchestrator digestProcessingOrchestrator,
    ISettingsManager settingsManager,
    IRssService rssService,
    ITaskScheduler<DigestId> taskScheduler,
    IDigestStepsService digestStepsService
) : IMainService
{
    public async Task<Result<DigestGenerationResultModelEnum>> ProcessDigest(
        DigestId digestId,
        DigestFilterModel filter,
        CancellationToken ct
    )
    {
        return await digestProcessingOrchestrator.ProcessDigest(digestId, filter, ct);
    }

    public Task<Result> QueueDigest(
        DigestId digestId,
        DigestFilterModel filter,
        CancellationToken ct
    )
    {
        return Task.FromResult(
            Result.Try(() => digestProcessingOrchestrator.QueueDigest(digestId, filter, ct))
        );
    }

    public Task<Result> CancelDigest(DigestId digestId)
    {
        return Task.FromResult(Result.Try(() => taskScheduler.CancelTaskInProgress(digestId)));
    }

    public Task<DigestId[]> GetInProgressDigests()
    {
        return Task.FromResult(taskScheduler.GetInProgressTasks());
    }

    public Task<DigestId[]> GetCancellationRequestedDigests()
    {
        return Task.FromResult(taskScheduler.GetCancellationRequestedTasks());
    }

    public Task<DigestId[]> GetWaitingDigests()
    {
        return Task.FromResult(taskScheduler.GetWaitingTasks());
    }

    public Task<Result> RemoveWaitingDigest(DigestId key)
    {
        return Task.FromResult(Result.Try(() => taskScheduler.RemoveWaitingTask(key)));
    }

    public async Task<Result<List<ChannelModel>>> GetChannels(CancellationToken ct)
    {
        return await channelsService.GetChannels(ct);
    }

    public async Task<Result> AddOrUpdateChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        return await channelsService.AddOrUpdateChannel(channelTgId, ct);
    }

    public async Task<Result> RemoveChannel(ChannelTgId channelTgId, CancellationToken ct)
    {
        return await channelsService.RemoveChannel(channelTgId, ct);
    }

    public async Task<Result<DigestSummaryModel[]>> GetDigestSummaries(CancellationToken ct)
    {
        return await digestService.GetDigestSummaries(ct);
    }

    public async Task<Result<DigestModel?>> GetDigest(DigestId digestId, CancellationToken ct)
    {
        return await digestService.GetDigest(digestId, ct);
    }

    public async Task<Result<SettingsModel>> GetSettings(CancellationToken ct)
    {
        return await settingsManager.LoadSettings(ct);
    }

    public async Task<Result> UpdateSettings(SettingsModel settings, CancellationToken ct)
    {
        return await settingsManager.SaveSettings(settings, ct);
    }

    public async Task<Result<SyndicationFeed>> GetRssFeed(CancellationToken ct)
    {
        return await rssService.GenerateRssFeed(ct);
    }

    public async Task<Result> DeleteDigest(DigestId digestId, CancellationToken ct)
    {
        return await digestService.DeleteDigest(digestId, ct);
    }

    public Task<Result<IDigestStepModel[]>> GetDigestSteps(DigestId digestId, CancellationToken ct)
    {
        return digestStepsService.GetAllSteps(digestId, ct);
    }

    [Obsolete("Will be removed before release")]
    public async Task<Result> SendDigestOverEmail(DigestId digestId, CancellationToken ct)
    {
        var digestResult = await digestService.GetDigest(digestId, ct);
        if (digestResult.IsFailed)
        {
            return Result.Fail(digestResult.Errors);
        }

        if (digestResult.Value is null)
        {
            return Result.Fail(new Error($"Failed to load digest {digestId} for email"));
        }

        var sendResult = await emailSender.SendDigest(digestResult.Value.DigestSummary, ct);
        return sendResult.IsFailed ? Result.Fail(sendResult.Errors) : Result.Ok();
    }
}
