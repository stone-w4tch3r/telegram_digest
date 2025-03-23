using FluentResults;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IDigestService
{
    /// <summary>
    /// Generate new digest within the specified time range
    /// </summary>
    Task<Result<DigestGenerationResultModelEnum>> GenerateDigest(
        DigestId digestId,
        DateOnly from,
        DateOnly to,
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
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        var channelsResult = await channelsRepository.LoadChannels(ct);
        if (channelsResult.IsFailed)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = channelsResult.Errors },
                ct
            );
            return Result.Fail(channelsResult.Errors);
        }

        await digestStepsService.AddStep(
            new RssReadingStartedStepModel
            {
                DigestId = digestId,
                Channels = channelsResult.Value.Select(x => x.TgId).ToArray(),
            },
            ct
        );

        var posts = new List<PostModel>();
        foreach (var channel in channelsResult.Value)
        {
            ct.ThrowIfCancellationRequested();
            var postsResult = await channelReader.FetchPosts(channel.TgId, from, to, ct);
            if (postsResult.IsSuccess)
            {
                posts.AddRange(postsResult.Value);
            }
        }

        if (posts.Count == 0)
        {
            logger.LogWarning("No posts found from [{from}] to [{to}] in any channel", from, to);
            await digestStepsService.AddStep(
                new SimpleStepModel()
                {
                    DigestId = digestId,
                    Type = DigestStepTypeModelEnum.NoPostsFound,
                },
                ct
            );
            return Result.Ok(DigestGenerationResultModelEnum.NoPosts);
        }

        await digestStepsService.AddStep(
            new RssReadingFinishedStepModel { DigestId = digestId, PostsCount = posts.Count },
            ct
        );

        var summaries = new List<PostSummaryModel>();
        foreach (var (post, i) in posts.Zip(Enumerable.Range(0, posts.Count)))
        {
            ct.ThrowIfCancellationRequested();
            var summaryResult = await aiSummarizer.GenerateSummary(post, ct);
            if (summaryResult.IsSuccess)
            {
                await digestStepsService.AddStep(
                    new AiProcessingStepModel
                    {
                        DigestId = digestId,
                        Percentage = i * 100 / posts.Count,
                    },
                    ct
                );
                summaries.Add(summaryResult.Value);
            }
            else
            {
                await digestStepsService.AddStep(
                    new ErrorStepModel { DigestId = digestId, Errors = summaryResult.Errors },
                    ct
                );
                return Result.Fail(summaryResult.Errors);
            }
        }

        ct.ThrowIfCancellationRequested();
        var digestSummaryResult = await aiSummarizer.GeneratePostsSummary(posts, ct);
        if (digestSummaryResult.IsFailed)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = digestSummaryResult.Errors },
                ct
            );
            return Result.Fail(digestSummaryResult.Errors);
        }

        await digestStepsService.AddStep(
            new AiProcessingStepModel { DigestId = digestId, Percentage = 100 },
            ct
        );

        var digest = new DigestModel(
            DigestId: digestId,
            PostsSummaries: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest, ct);
        if (!saveResult.IsSuccess)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = saveResult.Errors },
                ct
            );
            return Result.Fail(saveResult.Errors);
        }

        await digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Success },
            ct
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
