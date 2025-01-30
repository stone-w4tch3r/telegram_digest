using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class SummaryGenerator( // IOpenAIService openAiService,
    SettingsManager settingsManager,
    ILogger<SummaryGenerator> logger
)
{
    // private readonly IOpenAIService _openAiService;

    // _openAiService = openAiService;

    /// <summary>
    /// Generates a concise summary of a post using OpenAI's GPT model
    /// </summary>
    internal async Task<Result<PostSummaryModel>> GenerateSummary(PostModel post)
    {
        var settings = await settingsManager.LoadSettings();
        if (settings.IsFailed)
        {
            return Result.Fail(settings.Errors);
        }

        try
        {
            // var prompt =
            //     $"Summarize this post in one sentence:\n\nTitle: {post.Title}\n\nContent: {post.Description}";

            // var completionResult = await _openAiService.Completions.CreateCompletion(
            //     new CompletionCreateRequest
            //     {
            //         Prompt = prompt,
            //         Model = settings.Value.OpenAiSettings.Model,
            //         MaxTokens = settings.Value.OpenAiSettings.MaxTokens,
            //     }
            // );

            // if (!completionResult.Successful)
            //     return Result.Fail(new Error("OpenAI API request failed"));

            // var importance = await EvaluatePostImportance(post);
            // if (importance.IsFailed)
            //     return Result.Fail(importance.Errors);

            // return Result.Ok(
            //     new PostSummaryModel(
            //         PostId: post.PostId,
            //         ChannelId: post.ChannelId,
            //         Summary: completionResult.Choices[0].Text.Trim(),
            //         Url: post.Url,
            //         PublishedAt: post.PublishedAt,
            //         Importance: importance.Value
            //     )
            // );

            return Result.Fail<PostSummaryModel>(new Error("Not implemented"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate summary for post {PostUrl}", post.Url);
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }

    /// <summary>
    /// Evaluates post importance based on content analysis
    /// </summary>
    internal async Task<Result<Importance>> EvaluatePostImportance(PostModel post)
    {
        try
        {
            // var prompt =
            //     $"Rate the importance of this post from 1 to 3 (1 - low, 2 - medium, 3 - high):\n\n{post.Title}\n\n{post.Description}\n\nJust return the number.";

            // var completionResult = await _openAiService.Completions.CreateCompletion(
            //     new CompletionCreateRequest
            //     {
            //         Prompt = prompt,
            //         Model = "text-davinci-003",
            //         MaxTokens = 1,
            //     }
            // );

            // if (!completionResult.Successful)
            //     return Result.Fail(new Error("OpenAI API request failed"));

            // var importance = int.Parse(completionResult.Choices[0].Text.Trim());
            // return Result.Ok(new ImportanceModel(importance));

            return Result.Fail<Importance>(new Error("Not implemented"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to evaluate importance for post {Url}", post.Url);
            return Result.Fail(new Error("Importance evaluation failed").CausedBy(ex));
        }
    }

    internal async Task<Result<DigestSummaryModel>> GeneratePostsSummary(List<PostModel> posts)
    {
        try
        {
            // var postsContent = string.Join(
            //     "\n\n",
            //     posts.Select(p => $"{p.Title}\n{p.Description}")
            // );
            // var prompt = $"Create a brief summary of these posts:\n\n{postsContent}";

            // var completionResult = await _openAiService.Completions.CreateCompletion(
            //     new CompletionCreateRequest
            //     {
            //         Prompt = prompt,
            //         Model = "text-davinci-003",
            //         MaxTokens = 200,
            //     }
            // );

            // if (!completionResult.Successful)
            //     return Result.Fail(new Error("OpenAI API request failed"));

            // return Result.Ok(
            //     new DigestSummaryModel(
            //         DigestId: DigestId.NewId(),
            //         Title: "Daily Digest",
            //         PostsSummary: completionResult.Choices[0].Text.Trim(),
            //         PostsCount: posts.Count,
            //         AverageImportance: 2, // Default value
            //         CreatedAt: DateTime.UtcNow,
            //         DateFrom: posts.Min(p => p.PublishedAt),
            //         DateTo: posts.Max(p => p.PublishedAt),
            //         ImageUrl: new Uri("https://placeholder.com/image.jpg")
            //     )
            // );

            return Result.Fail<DigestSummaryModel>(new Error("Not implemented"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate posts summary");
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }
}
