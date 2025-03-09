using FluentResults;

namespace TelegramDigest.Backend.Core;

internal interface IDigestService
{
    /// <summary>
    /// Generate new digest within the specified time range
    /// </summary>
    public Task<Result<DigestGenerationResultTypeModelEnum>> GenerateDigest(
        DigestId digestId,
        DateOnly from,
        DateOnly to
    );

    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel?>> GetDigest(DigestId digestId);

    /// <summary>
    /// Loads all digest summaries (metadata for each digest) without posts
    /// </summary>
    public Task<Result<DigestSummaryModel[]>> GetDigestSummaries();

    /// <summary>
    /// Loads all digests including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel[]>> GetAllDigests();

    /// <summary>
    /// Deletes a digest and all associated posts.
    /// </summary>
    public Task<Result> DeleteDigest(DigestId digestId);
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
    public async Task<Result<DigestGenerationResultTypeModelEnum>> GenerateDigest(
        DigestId digestId,
        DateOnly from,
        DateOnly to
    )
    {
        var channelsResult = await channelsRepository.LoadChannels();
        if (channelsResult.IsFailed)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = channelsResult.Errors }
            );
            return Result.Fail(channelsResult.Errors);
        }

        await digestStepsService.AddStep(
            new RssReadingStartedStepModel
            {
                DigestId = digestId,
                Channels = channelsResult.Value.Select(x => x.TgId).ToArray(),
            }
        );

        var posts = new List<PostModel>();
        foreach (var channel in channelsResult.Value)
        {
            var postsResult = await channelReader.FetchPosts(channel.TgId, from, to);
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
                }
            );
            return Result.Ok(DigestGenerationResultTypeModelEnum.NoPosts);
        }

        await digestStepsService.AddStep(
            new RssReadingFinishedStepModel { DigestId = digestId, PostsCount = posts.Count }
        );

        var summaries = new List<PostSummaryModel>();
        foreach (var (post, i) in posts.Zip(Enumerable.Range(0, posts.Count)))
        {
            var summaryResult = await aiSummarizer.GenerateSummary(post);
            if (summaryResult.IsSuccess)
            {
                await digestStepsService.AddStep(
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
                await digestStepsService.AddStep(
                    new ErrorStepModel { DigestId = digestId, Errors = summaryResult.Errors }
                );
                return Result.Fail(summaryResult.Errors);
            }
        }

        var digestSummaryResult = await aiSummarizer.GeneratePostsSummary(posts);
        if (digestSummaryResult.IsFailed)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = digestSummaryResult.Errors }
            );
            return Result.Fail(digestSummaryResult.Errors);
        }
        await digestStepsService.AddStep(
            new AiProcessingStepModel { DigestId = digestId, Percentage = 100 }
        );

        var digest = new DigestModel(
            DigestId: digestId,
            PostsSummaries: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest);
        if (!saveResult.IsSuccess)
        {
            await digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Errors = saveResult.Errors }
            );
            return Result.Fail(saveResult.Errors);
        }

        await digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Success }
        );
        return Result.Ok(DigestGenerationResultTypeModelEnum.Success);
    }

    public async Task<Result<DigestModel?>> GetDigest(DigestId digestId)
    {
        return await digestRepository.LoadDigest(digestId);
    }

    public async Task<Result<DigestSummaryModel[]>> GetDigestSummaries()
    {
        return await digestRepository.LoadAllDigestSummaries();
    }

    public async Task<Result<DigestModel[]>> GetAllDigests()
    {
        return await digestRepository.LoadAllDigests();
    }

    public async Task<Result> DeleteDigest(DigestId digestId)
    {
        return await digestRepository.DeleteDigest(digestId);
    }
}
