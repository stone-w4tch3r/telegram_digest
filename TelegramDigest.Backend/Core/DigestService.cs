using FluentResults;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IDigestService
{
    /// <summary>
    /// Generate new digest based on filter parameters
    /// </summary>
    Task<Result<DigestGenerationResultModelEnum>> GenerateDigest(
        DigestId digestId,
        DigestFilterModel filter,
        CancellationToken ct
    );

    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    Task<Result<DigestModel?>> GetDigest(DigestId digestId, CancellationToken ct);

    /// <summary>
    /// Loads all digest summaries (metadata for each digest) without posts
    /// </summary>
    Task<Result<DigestSummaryModel[]>> GetDigestSummaries(CancellationToken ct);

    /// <summary>
    /// Loads all digests including all post summaries and metadata
    /// </summary>
    Task<Result<DigestModel[]>> GetAllDigests(CancellationToken ct);

    /// <summary>
    /// Deletes a digest and all associated posts.
    /// </summary>
    Task<Result> DeleteDigest(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestService(
    IDigestRepository digestRepository,
    IChannelReader channelReader,
    IChannelsRepository channelsRepository,
    IAiSummarizer aiSummarizer,
    IDigestStepsService digestStepsService,
    ILogger<DigestService> logger
) : IDigestService
{
    /// <summary>
    /// Creates a new digest from posts within the specified time range
    /// </summary>
    /// <returns>Type of digest generation result or error if generation failed</returns>
    public async Task<Result<DigestGenerationResultModelEnum>> GenerateDigest(
        DigestId digestId,
        DigestFilterModel filter,
        CancellationToken ct
    )
    {
        var channelsResult = await channelsRepository.LoadChannels(ct);
        if (channelsResult.IsFailed)
        {
            digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = channelsResult.Errors }
            );
            return Result.Fail(channelsResult.Errors);
        }

        var channels =
            filter.SelectedChannels != null
                ? [.. channelsResult.Value.Where(c => filter.SelectedChannels.Contains(c.TgId))]
                : channelsResult.Value;

        digestStepsService.AddStep(
            new RssReadingStartedStepModel
            {
                DigestId = digestId,
                Channels = channels.Select(x => x.TgId).ToArray(),
            }
        );
        
        if (channels.Count == 0)
        {
            const string Message = "No channels selected";
            logger.LogError(Message);
            digestStepsService.AddStep(
                new ErrorStepModel
                {
                    DigestId = digestId,
                    Errors = [new Error(Message)],
                    Message = Message,
                }
            );
            return Result.Fail(new Error(Message));
        }

        var posts = new List<PostModel>();
        var errorsByChannel = new Dictionary<ChannelTgId, List<IError>>();

        foreach (var channel in channels)
        {
            ct.ThrowIfCancellationRequested();
            var postsResult = await channelReader.FetchPosts(
                channel.TgId,
                filter.DateFrom,
                filter.DateTo,
                ct
            );
            if (postsResult.IsSuccess)
            {
                posts.AddRange(postsResult.Value);
            }
            else
            {
                errorsByChannel[channel.TgId] = postsResult.Errors;
            }
        }

        if (errorsByChannel.Count == channels.Count)
        {
            const string Message = "Failed to read all channels";
            var errors =
                (List<IError>)[new Error(Message), .. errorsByChannel.Values.SelectMany(x => x)];
            logger.LogError(Message);
            digestStepsService.AddStep(
                new ErrorStepModel
                {
                    DigestId = digestId,
                    Errors = errors,
                    Message = Message,
                }
            );
            return Result.Fail(errors);
        }

        if (posts.Count == 0)
        {
            logger.LogWarning(
                "No posts found from [{from}] to [{to}] in any channel",
                filter.DateFrom,
                filter.DateTo
            );
            digestStepsService.AddStep(
                new SimpleStepModel
                {
                    DigestId = digestId,
                    Type = DigestStepTypeModelEnum.NoPostsFound,
                }
            );
            return Result.Ok(DigestGenerationResultModelEnum.NoPosts);
        }

        digestStepsService.AddStep(
            new RssReadingFinishedStepModel { DigestId = digestId, PostsCount = posts.Count }
        );

        var summaries = new List<PostSummaryModel>();
        foreach (var (post, i) in posts.Zip(Enumerable.Range(0, posts.Count)))
        {
            ct.ThrowIfCancellationRequested();
            var summaryResult = await aiSummarizer.GenerateSummary(post, ct);
            if (summaryResult.IsSuccess)
            {
                digestStepsService.AddStep(
                    new AiProcessingStepModel
                    {
                        DigestId = digestId,
                        Percentage = i * 100 / posts.Count,
                    }
                );
                summaries.Add(summaryResult.Value);
            }
            else
            {
                digestStepsService.AddStep(
                    new ErrorStepModel { DigestId = digestId, Errors = summaryResult.Errors }
                );
                return Result.Fail(summaryResult.Errors);
            }
        }

        ct.ThrowIfCancellationRequested();
        var digestSummaryResult = await aiSummarizer.GeneratePostsSummary(posts, ct);
        if (digestSummaryResult.IsFailed)
        {
            digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = digestSummaryResult.Errors }
            );
            return Result.Fail(digestSummaryResult.Errors);
        }

        digestStepsService.AddStep(
            new AiProcessingStepModel { DigestId = digestId, Percentage = 100 }
        );

        var digest = new DigestModel(
            DigestId: digestId,
            PostsSummaries: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest, ct);
        if (!saveResult.IsSuccess)
        {
            digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = saveResult.Errors }
            );
            return Result.Fail(saveResult.Errors);
        }

        digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Success }
        );
        return Result.Ok(DigestGenerationResultModelEnum.Success);
    }

    public async Task<Result<DigestModel?>> GetDigest(DigestId digestId, CancellationToken ct)
    {
        return await digestRepository.LoadDigest(digestId, ct);
    }

    public async Task<Result<DigestSummaryModel[]>> GetDigestSummaries(CancellationToken ct)
    {
        return await digestRepository.LoadAllDigestSummaries(ct);
    }

    public async Task<Result<DigestModel[]>> GetAllDigests(CancellationToken ct)
    {
        return await digestRepository.LoadAllDigests(ct);
    }

    public async Task<Result> DeleteDigest(DigestId digestId, CancellationToken ct)
    {
        return await digestRepository.DeleteDigest(digestId, ct);
    }
}
