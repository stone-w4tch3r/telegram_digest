using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class DigestsService(
    DigestRepository digestRepository,
    ChannelReader channelReader,
    ChannelsRepository channelsRepository,
    SummaryGenerator summaryGenerator,
    ILogger<DigestsService> logger
)
{
    private readonly ILogger<DigestsService> _logger = logger;

    /// <summary>
    /// Creates a new digest from posts within the specified time range
    /// </summary>
    /// <returns>DigestId of the generated digest or error if generation failed</returns>
    internal async Task<Result<DigestId?>> GenerateDigest(DateOnly from, DateOnly to)
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
            _logger.LogWarning("No posts found from [{from}] to [{to}] in any channel", from, to);
            return Result.Ok();
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

        var digestId = DigestId.NewId();
        var digest = new DigestModel(
            DigestId: digestId,
            PostsSummaries: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest);
        return saveResult.IsSuccess
            ? Result.Ok((DigestId?)digestId)
            : Result.Fail(saveResult.Errors);
    }

    internal async Task<Result<DigestModel>> GetDigest(DigestId digestId)
    {
        return await digestRepository.LoadDigest(digestId);
    }

    internal async Task<Result<List<DigestSummaryModel>>> GetDigestSummaries()
    {
        return await digestRepository.LoadAllDigestSummaries();
    }
}
