using FluentResults;

namespace TelegramDigest.Backend.Core;

internal interface IDigestService
{
    /// <summary>
    /// Generate new digest within the specified time range
    /// </summary>
    public Task<Result<DigestGenerationStatusModel>> GenerateDigest(
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
    ISummaryGenerator summaryGenerator,
    ILogger<DigestService> logger
) : IDigestService
{
    /// <summary>
    /// Creates a new digest from posts within the specified time range
    /// </summary>
    /// <returns>DigestId of the generated digest or error if generation failed</returns>
    public async Task<Result<DigestGenerationStatusModel>> GenerateDigest(
        DigestId digestId,
        DateOnly from,
        DateOnly to
    )
    {
        var channels = await channelsRepository.LoadChannels();
        if (channels.IsFailed)
        {
            return Result.Fail(channels.Errors);
        }

        var posts = new List<PostModel>();
        foreach (var channel in channels.Value)
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
            return Result.Ok(DigestGenerationStatusModel.NoPosts);
        }

        var summaries = new List<PostSummaryModel>();
        foreach (var post in posts)
        {
            var summaryResult = await summaryGenerator.GenerateSummary(post);
            if (summaryResult.IsSuccess)
            {
                summaries.Add(summaryResult.Value);
            }
        }

        var digestSummaryResult = await summaryGenerator.GeneratePostsSummary(posts);
        if (digestSummaryResult.IsFailed)
        {
            return Result.Fail(digestSummaryResult.Errors);
        }

        var digest = new DigestModel(
            DigestId: digestId,
            PostsSummaries: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest);
        return saveResult.IsSuccess
            ? Result.Ok(DigestGenerationStatusModel.Success)
            : Result.Fail(saveResult.Errors);
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
