using FluentResults;
using OpenAI.Chat;

namespace TelegramDigest.Application.Services;

internal interface ISummaryGenerator
{
    public Task<Result<PostSummaryModel>> GenerateSummary(PostModel post);
    public Task<Result<DigestSummaryModel>> GeneratePostsSummary(List<PostModel> posts);
}

internal sealed class SummaryGenerator(
    ISettingsManager settingsManager,
    ILogger<SummaryGenerator> logger
) : ISummaryGenerator
{
    private ChatClient? _chatClient;

    private async Task<Result<ChatClient>> GetChatClient()
    {
        if (_chatClient != null)
            return Result.Ok(_chatClient);

        var settings = await settingsManager.LoadSettings();
        if (settings.IsFailed)
        {
            return Result.Fail(settings.Errors);
        }

        _chatClient = new(
            model: settings.Value.OpenAiSettings.Model,
            apiKey: settings.Value.OpenAiSettings.ApiKey
        );

        return Result.Ok(_chatClient);
    }

    public async Task<Result<PostSummaryModel>> GenerateSummary(PostModel post)
    {
        try
        {
            var clientResult = await GetChatClient();
            var client = clientResult.Value;
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(
                        "You are a helpful assistant that creates concise one-sentence summaries of posts."
                    ),
                    new UserChatMessage(
                        $"Summarize this post in one sentence:\n\n{post.HtmlContent}"
                    ),
                ];

            var completionResult = await client.CompleteChatAsync(messages);

            var importance = await EvaluatePostImportance(post);
            if (importance.IsFailed)
            {
                return Result.Fail(importance.Errors);
            }

            return Result.Ok(
                new PostSummaryModel(
                    ChannelTgId: post.ChannelTgId,
                    Summary: completionResult.Value.Content[0].Text.Trim(),
                    Url: post.Url,
                    PublishedAt: post.PublishedAt,
                    Importance: importance.Value
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate summary for post {PostUrl}", post.Url);
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }

    private async Task<Result<Importance>> EvaluatePostImportance(PostModel post)
    {
        try
        {
            var clientResult = await GetChatClient();
            var client = clientResult.Value;
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(
                        "You are an assistant that evaluates content importance. Respond only with a number: 1 for low importance, 2 for medium importance, or 3 for high importance."
                    ),
                    new UserChatMessage(
                        $"Rate the importance of this post from 1 to 3:\n\n{post.HtmlContent}"
                    ),
                ];

            var completion = await client.CompleteChatAsync(messages);
            var importanceValue = int.Parse(completion.Value.Content[0].Text.Trim());

            return Result.Ok(new Importance(importanceValue));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to evaluate importance for post {Url}", post.Url);
            return Result.Fail(new Error("Importance evaluation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestSummaryModel>> GeneratePostsSummary(List<PostModel> posts)
    {
        try
        {
            var clientResult = await GetChatClient();
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var postsContent = string.Join("\n\n", posts.Select(p => p.HtmlContent));

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(
                        "You are a helpful assistant that creates brief summaries of multiple posts, highlighting key themes and important information."
                    ),
                    new UserChatMessage(
                        $"Create a brief summary of these posts:\n\n{postsContent}"
                    ),
                ];

            var completion = await clientResult.Value.CompleteChatAsync(messages);

            return Result.Ok(
                new DigestSummaryModel(
                    DigestId: DigestId.NewId(),
                    Title: "Daily Digest",
                    PostsSummary: completion.Value.Content[0].Text.Trim(),
                    PostsCount: posts.Count,
                    AverageImportance: posts.Count > 0
                        ? await CalculateAverageImportance(posts)
                        : 0,
                    CreatedAt: DateTime.UtcNow,
                    DateFrom: posts.Min(p => p.PublishedAt),
                    DateTo: posts.Max(p => p.PublishedAt)
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate posts summary");
            return Result.Fail(new Error("Summary generation failed").CausedBy(ex));
        }
    }

    private async Task<double> CalculateAverageImportance(List<PostModel> posts)
    {
        var importanceResults = await Task.WhenAll(posts.Select(EvaluatePostImportance));

        var successfulResults = importanceResults
            .Where(r => r.IsSuccess)
            .Select(r => r.Value.Value)
            .ToArray();

        return successfulResults.Length != 0 ? successfulResults.Average() : 0;
    }
}
