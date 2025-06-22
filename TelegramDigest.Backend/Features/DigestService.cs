using FluentResults;
using TelegramDigest.Backend.Db;
using TelegramDigest.Backend.Features.DigestSteps;
using TelegramDigest.Backend.Models;

namespace TelegramDigest.Backend.Features;

internal interface IDigestService
{
    /// <summary>
    /// Generate new digest based on parameters
    /// </summary>
    Task<Result<DigestGenerationResultModelEnum>> GenerateDigest(
        DigestId digestId,
        DigestParametersModel parameters,
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
    IFeedReader feedReader,
    IFeedsRepository feedsRepository,
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
        DigestParametersModel parameters,
        CancellationToken ct
    )
    {
        var feedsResult = await feedsRepository.LoadFeeds(ct);
        if (feedsResult.IsFailed)
        {
            digestStepsService.AddStep(
                new ErrorStepModel { DigestId = digestId, Message = "Failed to load feeds" }
            );
            return Result.Fail(feedsResult.Errors);
        }

        if (parameters.SelectedFeeds is { Count: 0 })
        {
            const string Message = "No feeds selected for digest generation";
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

        var feeds =
            parameters.SelectedFeeds != null
                ? [.. feedsResult.Value.Where(f => parameters.SelectedFeeds.Contains(f.FeedUrl))]
                : feedsResult.Value;

        digestStepsService.AddStep(
            new RssReadingStartedStepModel
            {
                DigestId = digestId,
                Feeds = feeds.Select(x => x.FeedUrl).ToArray(),
            }
        );

        var posts = new List<PostModel>();
        var errorsByFeed = new Dictionary<FeedUrl, List<IError>>();

        foreach (var feed in feeds)
        {
            ct.ThrowIfCancellationRequested();
            var postsResult = await feedReader.FetchPosts(
                feed.FeedUrl,
                parameters.DateFrom,
                parameters.DateTo,
                ct
            );
            if (postsResult.IsSuccess)
            {
                posts.AddRange(postsResult.Value);
            }
            else
            {
                errorsByFeed[feed.FeedUrl] = postsResult.Errors;
            }
        }

        if (errorsByFeed.Count == feeds.Count())
        {
            const string Message = "Failed to read all feeds";
            var errors =
                (List<IError>)[new Error(Message), .. errorsByFeed.Values.SelectMany(x => x)];
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
                "No posts found from [{from}] to [{to}] in any feed",
                parameters.DateFrom,
                parameters.DateTo
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
        var usedPrompts = new Dictionary<PromptTypeEnumModel, string>
        {
            [PromptTypeEnumModel.PostSummary] =
                parameters.PostSummaryUserPromptOverride?.Text ?? string.Empty,
            [PromptTypeEnumModel.PostImportance] =
                parameters.PostImportanceUserPromptOverride?.Text ?? string.Empty,
        };

        foreach (var (post, i) in posts.Zip(Enumerable.Range(0, posts.Count)))
        {
            ct.ThrowIfCancellationRequested();
            var summaryResult = await aiSummarizer.GenerateSummary(
                post,
                parameters.PostSummaryUserPromptOverride,
                parameters.PostImportanceUserPromptOverride,
                ct
            );
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
        usedPrompts[PromptTypeEnumModel.DigestSummary] =
            parameters.DigestSummaryUserPromptOverride?.Text ?? string.Empty;

        var digestSummaryResult = await aiSummarizer.GeneratePostsSummary(
            posts,
            parameters.DigestSummaryUserPromptOverride,
            parameters.PostImportanceUserPromptOverride,
            ct
        );
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
            DigestSummary: digestSummaryResult.Value,
            UsedPrompts: usedPrompts
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
