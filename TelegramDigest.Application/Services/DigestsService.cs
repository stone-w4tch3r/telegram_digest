using FluentResults;
using Microsoft.Extensions.Logging;

namespace TelegramDigest.Application.Services;

public class DigestsService
{
    private readonly DigestRepository _digestRepository;
    private readonly ChannelReader _channelReader;
    private readonly ChannelsRepository _channelsRepository;
    private readonly SummaryGenerator _summaryGenerator;
    private readonly ILogger<DigestsService> _logger;

    public DigestsService(
        DigestRepository digestRepository,
        ChannelReader channelReader,
        ChannelsRepository channelsRepository,
        SummaryGenerator summaryGenerator,
        ILogger<DigestsService> logger
    )
    {
        _digestRepository = digestRepository;
        _channelReader = channelReader;
        _channelsRepository = channelsRepository;
        _summaryGenerator = summaryGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new digest from posts within the specified time range
    /// </summary>
    /// <returns>DigestId of the generated digest or error if generation failed</returns>
    public async Task<Result<DigestId>> GenerateDigest(DateTime from, DateTime to)
    {
        var channels = await _channelsRepository.LoadChannels();
        if (channels.IsFailed)
            return channels.ToResult<DigestId>();

        var posts = new List<PostModel>();
        foreach (var channel in channels.Value)
        {
            var postsResult = await _channelReader.FetchPosts(channel.ChannelId);
            if (postsResult.IsSuccess)
            {
                posts.AddRange(
                    postsResult.Value.Where(p => p.PublishedAt >= from && p.PublishedAt <= to)
                );
            }
        }

        if (!posts.Any())
            return Result.Fail(new Error("No posts found in the specified time range"));

        var summaries = new List<PostSummaryModel>();
        foreach (var post in posts)
        {
            var summaryResult = await _summaryGenerator.GenerateSummary(post);
            if (summaryResult.IsSuccess)
            {
                summaries.Add(summaryResult.Value);
            }
        }

        var digestSummaryResult = await _summaryGenerator.GeneratePostsSummary(posts);
        if (digestSummaryResult.IsFailed)
            return digestSummaryResult.ToResult<DigestId>();

        var digestId = DigestId.NewId();
        var digest = new DigestModel(
            DigestId: digestId,
            Posts: summaries,
            DigestSummary: digestSummaryResult.Value
        );

        var saveResult = await _digestRepository.SaveDigest(digest);
        return saveResult.IsSuccess ? Result.Ok(digestId) : saveResult.ToResult<DigestId>();
    }

    public async Task<Result<DigestModel>> GetDigest(DigestId digestId)
    {
        return await _digestRepository.LoadDigest(digestId);
    }

    public async Task<Result<List<DigestSummaryModel>>> GetDigestsSummaries()
    {
        return await _digestRepository.LoadAllDigestSummaries();
    }
}
