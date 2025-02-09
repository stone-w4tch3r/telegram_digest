using FluentResults;
using OpenAI.Chat;

namespace TelegramDigest.Backend.Core;

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
    private (string Model, string ApiKey, Uri Endpoint)? _lastClientSettings;

    private async Task<Result<ChatClient>> GetChatClient()
    {
        var settingsResult = await settingsManager.LoadSettings();
        if (settingsResult.IsFailed)
        {
            return Result.Fail(settingsResult.Errors);
        }

        if (
            _chatClient != null
            && _lastClientSettings != null
            && settingsResult.Value.OpenAiSettings.Model == _lastClientSettings.Value.Model
            && settingsResult.Value.OpenAiSettings.ApiKey == _lastClientSettings.Value.ApiKey
            && settingsResult.Value.OpenAiSettings.Endpoint == _lastClientSettings.Value.Endpoint
        )
        {
            return Result.Ok(_chatClient);
        }

        _chatClient = new(
            model: settingsResult.Value.OpenAiSettings.Model,
            credential: new(settingsResult.Value.OpenAiSettings.ApiKey),
            options: new() { Endpoint = settingsResult.Value.OpenAiSettings.Endpoint }
        );

        _lastClientSettings = (
            settingsResult.Value.OpenAiSettings.Model,
            settingsResult.Value.OpenAiSettings.ApiKey,
            settingsResult.Value.OpenAiSettings.Endpoint
        );

        return Result.Ok(_chatClient);
    }

    public async Task<Result<PostSummaryModel>> GenerateSummary(PostModel post)
    {
        try
        {
            var settingsResult = await settingsManager.LoadSettings();
            if (settingsResult.IsFailed)
            {
                return Result.Fail(settingsResult.Errors);
            }
            var prompts = settingsResult.Value.PromptSettings;

            var clientResult = await GetChatClient();
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(prompts.PostSummarySystemPrompt),
                    new UserChatMessage(
                        prompts.PostSummaryUserPrompt.ReplacePlaceholder(
                            post.HtmlContent.HtmlString
                        )
                    ),
                ];

            var completionResult = await clientResult.Value.CompleteChatAsync(messages);

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
            var settingsResult = await settingsManager.LoadSettings();
            if (settingsResult.IsFailed)
            {
                return Result.Fail(settingsResult.Errors);
            }
            var prompts = settingsResult.Value.PromptSettings;

            var clientResult = await GetChatClient();
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(prompts.PostImportanceSystemPrompt),
                    new UserChatMessage(
                        prompts.PostImportanceUserPrompt.ReplacePlaceholder(
                            post.HtmlContent.HtmlString
                        )
                    ),
                ];

            var completion = await clientResult.Value.CompleteChatAsync(messages);
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
            var settingsResult = await settingsManager.LoadSettings();
            if (settingsResult.IsFailed)
            {
                return Result.Fail(settingsResult.Errors);
            }
            var prompts = settingsResult.Value.PromptSettings;

            var clientResult = await GetChatClient();
            if (clientResult.IsFailed)
            {
                return Result.Fail(clientResult.Errors);
            }

            var postsContent = string.Join("\n\n", posts.Select(p => p.HtmlContent));

            var messages = (ChatMessage[])
                [
                    new SystemChatMessage(prompts.DigestSummarySystemPrompt),
                    new UserChatMessage(
                        prompts.DigestSummaryUserPrompt.ReplacePlaceholder(postsContent)
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
