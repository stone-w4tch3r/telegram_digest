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
    internal async Task<Result<DigestId>> GenerateDigest(DateTime from, DateTime to)
    {
        var channels = await channelsRepository.LoadChannels();
        if (channels.IsFailed)
            return channels.ToResult<DigestId>();

        var posts = new List<PostModel>();
        foreach (var channel in channels.Value)
        {
            var postsResult = await channelReader.FetchPosts(channel.ChannelId, from, to);
            if (postsResult.IsSuccess)
            {
                posts.AddRange(postsResult.Value);
            }
        }

        if (posts.Count == 0)
            return Result.Fail(new Error("No posts found in the specified time range"));

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
            return digestSummaryResult.ToResult<DigestId>();

        var digestId = DigestId.NewId();
        var digest = new DigestModel(
            DigestId: digestId,
            Posts: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await digestRepository.SaveDigest(digest);
        return saveResult.IsSuccess ? Result.Ok(digestId) : saveResult.ToResult<DigestId>();
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
