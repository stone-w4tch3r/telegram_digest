using FluentResults;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace TelegramDigest.Application.Services;

public class SummaryGenerator
{
    private readonly IOpenAIService _openAiService;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<SummaryGenerator> _logger;

    public SummaryGenerator(
        IOpenAIService openAiService,
        SettingsManager settingsManager,
        ILogger<SummaryGenerator> logger
    )
    {
        _openAiService = openAiService;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    /// <summary>
    /// Generates a concise summary of a post using OpenAI's GPT model
    /// </summary>
    public async Task<Result<PostSummaryModel>> GenerateSummary(PostModel post)
    {
        var settings = await _settingsManager.LoadSettings();
        if (settings.IsFailed)
            return settings.ToResult<PostSummaryModel>();

        try
        {
            var prompt = $"Summarize this post in one sentence:\n\nTitle: {post.Title}\n\nContent: {post.Description}";

            var completionResult = await _openAiService.Completions.CreateCompletion(new CompletionCreateRequest
            {
                Prompt = prompt,
                Model = settings.Value.OpenAiSettings.Model,
                MaxTokens = settings.Value.OpenAiSettings.MaxTokens
            });

            if (!completionResult.Successful)
                return Result.Fail(new Error("OpenAI API request failed"));

            var importance = await EvaluatePostImportance(post);
            if (importance.IsFailed)
                return importance.ToResult<PostSummaryModel>();

            return Result.Ok(new PostSummaryModel(
                PostId: post.PostId,
                ChannelId: post.ChannelId,
                Summary: completionResult.Choices[0].Text.Trim(),
                Url: post.Url,
                PublishedAt: post.PublishedAt,
                Importance: importance.Value
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary for post {PostId}", post.PostId);
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }

    /// <summary>
    /// Evaluates post importance based on content analysis
    /// </summary>
    public async Task<Result<ImportanceModel>> EvaluatePostImportance(PostModel post)
    {
        try
        {
            var prompt = $"Rate the importance of this post from 1 to 3 (1 - low, 2 - medium, 3 - high):\n\n{post.Title}\n\n{post.Description}\n\nJust return the number.";

            var completionResult = await _openAiService.Completions.CreateCompletion(new CompletionCreateRequest
            {
                Prompt = prompt,
                Model = "text-davinci-003",
                MaxTokens = 1
            });

            if (!completionResult.Successful)
                return Result.Fail(new Error("OpenAI API request failed"));

            var importance = int.Parse(completionResult.Choices[0].Text.Trim());
            return Result.Ok(new ImportanceModel(importance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate importance for post {PostId}", post.PostId);
            return Result.Fail(new Error("Importance evaluation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestSummaryModel>> GeneratePostsSummary(List<PostModel> posts)
    {
        try
        {
            var postsContent = string.Join("\n\n", posts.Select(p => $"{p.Title}\n{p.Description}"));
            var prompt = $"Create a brief summary of these posts:\n\n{postsContent}";

            var completionResult = await _openAiService.Completions.CreateCompletion(new CompletionCreateRequest
            {
                Prompt = prompt,
                Model = "text-davinci-003",
                MaxTokens = 200
            });

            if (!completionResult.Successful)
                return Result.Fail(new Error("OpenAI API request failed"));

            return Result.Ok(new DigestSummaryModel(
                DigestId: DigestId.NewId(),
                Title: "Daily Digest",
                PostsSummary: completionResult.Choices[0].Text.Trim(),
                PostsCount: posts.Count,
                AverageImportance: 2, // Default value
                CreatedAt: DateTime.UtcNow,
                DateFrom: posts.Min(p => p.PublishedAt),
                DateTo: posts.Max(p => p.PublishedAt),
                ImageUrl: new Uri("https://placeholder.com/image.jpg")
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate posts summary");
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }
}
