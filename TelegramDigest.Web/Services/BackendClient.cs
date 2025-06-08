using System.Diagnostics;
using System.ServiceModel.Syndication;
using FluentResults;
using TelegramDigest.Backend.Features;
using TelegramDigest.Backend.Features.DigestSteps;
using TelegramDigest.Backend.Models;
using TelegramDigest.Web.Models.ViewModels;

namespace TelegramDigest.Web.Services;

public sealed class BackendClient(IMainService mainService, ILogger<BackendClient> logger)
{
    public async Task<Result<List<DigestSummaryViewModel>>> GetDigestSummaries()
    {
        var result = await mainService.GetDigestSummaries(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digests: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }

        return Result.Ok(
            result
                .Value.Select(d => new DigestSummaryViewModel
                {
                    Id = d.DigestId.Guid,
                    Title = d.Title,
                    Summary = d.PostsSummary,
                    PostsCount = d.PostsCount,
                    AverageImportance = d.AverageImportance,
                    CreatedAt = d.CreatedAt,
                    DateFrom = d.DateFrom,
                    DateTo = d.DateTo,
                })
                .ToList()
        );
    }

    public async Task<
        Result<(DigestSummaryViewModel summary, PostSummaryViewModel[] posts)>
    > GetDigest(Guid id)
    {
        var result = await mainService.GetDigest(new(id), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digest {DigestId}: {Errors}", id, result.Errors);
            return Result.Fail(result.Errors);
        }

        if (result.Value is null)
        {
            return Result.Fail("Digest not found");
        }

        return Result.Ok(
            (
                new DigestSummaryViewModel
                {
                    Id = result.Value.DigestId.Guid,
                    Title = result.Value.DigestSummary.Title,
                    Summary = result.Value.DigestSummary.PostsSummary,
                    PostsCount = result.Value.DigestSummary.PostsCount,
                    AverageImportance = result.Value.DigestSummary.AverageImportance,
                    CreatedAt = result.Value.DigestSummary.CreatedAt,
                    DateFrom = result.Value.DigestSummary.DateFrom,
                    DateTo = result.Value.DigestSummary.DateTo,
                },
                result
                    .Value.PostsSummaries.Select(p => new PostSummaryViewModel
                    {
                        FeedUrl = p.FeedUrl.ToString(),
                        Summary = p.Summary,
                        Url = p.Url.ToString(),
                        PostedAt = p.PublishedAt,
                        Importance = p.Importance.Number,
                    })
                    .ToArray()
            )
        );
    }

    public async Task<Result> DeleteDigest(Guid id)
    {
        return await mainService.DeleteDigest(new(id), CancellationToken.None);
    }

    public async Task<Result<Guid>> QueueDigest(
        DateTime dateFrom,
        DateTime dateTo,
        string[] selectedFeeds
    )
    {
        var digestId = Guid.NewGuid();
        var parameters = new DigestParametersModel(
            DateFrom: DateOnly.FromDateTime(dateFrom),
            DateTo: DateOnly.FromDateTime(dateTo),
            SelectedFeeds: selectedFeeds.Select(f => new FeedUrl(f)).ToHashSet()
        );

        var digestResult = await mainService.QueueDigest(
            new(digestId),
            parameters,
            CancellationToken.None
        );

        if (digestResult.IsFailed)
        {
            logger.LogError("Failed to generate digest: {Errors}", digestResult.Errors);
            return Result.Fail(digestResult.Errors);
        }

        return Result.Ok(digestId);
    }

    public async Task<Result<List<FeedViewModel>>> GetFeeds()
    {
        var result = await mainService.GetFeeds(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get feeds: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }

        return Result.Ok(
            result
                .Value.Select(f => new FeedViewModel
                {
                    Title = f.Title,
                    Url = f.FeedUrl.ToString(),
                })
                .ToList()
        );
    }

    public async Task<Result> AddOrUpdateFeed(AddFeedViewModel feed, CancellationToken ct)
    {
        return await mainService.AddOrUpdateFeed(new(feed.FeedUrl), ct);
    }

    public async Task<Result> DeleteFeedAsync(string feedUrl)
    {
        return await mainService.RemoveFeed(new(feedUrl), CancellationToken.None);
    }

    public async Task<Result<SettingsViewModel>> GetSettings()
    {
        var result = await mainService.GetSettings(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get settings: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }

        var settings = result.Value;
        return Result.Ok(
            new SettingsViewModel
            {
                RecipientEmail = settings.EmailRecipient,
                SmtpHost = settings.SmtpSettings.Host,
                SmtpPort = settings.SmtpSettings.Port,
                SmtpUsername = settings.SmtpSettings.Username,
                SmtpPassword = settings.SmtpSettings.Password,
                SmtpUseSsl = settings.SmtpSettings.UseSsl,
                OpenAiApiKey = settings.OpenAiSettings.ApiKey,
                OpenAiModel = settings.OpenAiSettings.Model,
                OpenAiMaxToken = settings.OpenAiSettings.MaxTokens,
                OpenAiEndpoint = settings.OpenAiSettings.Endpoint,
                DigestTimeUtc = settings.DigestTime.Time,
                PromptDigestSummaryUser = settings.PromptSettings.DigestSummaryUserPrompt,
                PromptPostSummaryUser = settings.PromptSettings.PostSummaryUserPrompt,
                PromptPostImportanceUser = settings.PromptSettings.PostImportanceUserPrompt,
            }
        );
    }

    public async Task<Result> UpdateSettings(SettingsViewModel settings)
    {
        var settingsModel = new SettingsModel(
            settings.RecipientEmail,
            new(settings.DigestTimeUtc),
            new(
                settings.SmtpHost,
                settings.SmtpPort,
                settings.SmtpUsername,
                settings.SmtpPassword,
                settings.SmtpUseSsl
            ),
            new(
                settings.OpenAiApiKey,
                settings.OpenAiModel,
                settings.OpenAiMaxToken,
                settings.OpenAiEndpoint
            ),
            new(
                new(settings.PromptPostSummaryUser),
                new(settings.PromptPostImportanceUser),
                new(settings.PromptDigestSummaryUser)
            )
        );

        var result = await mainService.UpdateSettings(settingsModel, CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to update settings: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }
        return Result.Ok();
    }

    public async Task<Result<SyndicationFeed>> GetRssFeed()
    {
        return await mainService.GetRssFeed(CancellationToken.None);
    }

    public async Task<Result<DigestProgressViewModel>> GetDigestProgress(Guid id)
    {
        var result = await mainService.GetDigestSteps(new(id), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digest statuses: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }

        var steps = result.Value.OrderBy(x => x.Timestamp).ToArray();
        var progressVm =
            steps.Length == 0
                ? new()
                {
                    Id = id,
                    StartedAt = null,
                    CompletedAt = null,
                    CurrentStep = null,
                    PercentComplete = 0,
                    Steps = [],
                    ErrorMessage = null,
                }
                : new DigestProgressViewModel
                {
                    Id = id,
                    StartedAt = steps[0].Timestamp,
                    CompletedAt = steps[^1].Type.MapToVm().IsFinished()
                        ? steps[^1].Timestamp
                        : null,
                    CurrentStep = steps.Length == 0 ? null : steps.Last().Type.MapToVm(),
                    PercentComplete = steps[^1].Type.MapToVm().IsFinished()
                        ? 100
                        : steps.GetLastAiStepOrDefault()?.Percentage ?? 1,
                    Steps = steps
                        .Select(x => new DigestStepViewModel
                        {
                            Type = x.Type.MapToVm(),
                            Message = x.Message,
                            Timestamp = x.Timestamp,
                            Feeds = x is RssReadingStartedStepModel readingStartedStep
                                ? readingStartedStep.Feeds.Select(f => f.ToString()).ToArray()
                                : null,
                            PostsCount = x is RssReadingFinishedStepModel readingFinishedStep
                                ? readingFinishedStep.PostsCount
                                : null,
                            PercentComplete = x is AiProcessingStepModel aiStep
                                ? aiStep.Percentage
                                : null,
                        })
                        .ToArray(),
                    ErrorMessage = steps[^1] is ErrorStepModel error
                        ? error.FormatErrorStep()
                        : null,
                };
        return Result.Ok(progressVm);
    }

    public async Task<Result> CancelDigest(Guid digestId)
    {
        return await mainService.CancelDigest(new(digestId));
    }

    public async Task<Guid[]> GetInProgressDigests()
    {
        return (await mainService.GetInProgressDigests()).Select(x => x.Guid).ToArray();
    }

    public async Task<Guid[]> GetWaitingDigests()
    {
        return (await mainService.GetWaitingDigests()).Select(x => x.Guid).ToArray();
    }

    public async Task<Guid[]> GetCancellationRequestedDigests()
    {
        return (await mainService.GetCancellationRequestedDigests()).Select(x => x.Guid).ToArray();
    }

    public async Task<Result> RemoveWaitingDigest(Guid key)
    {
        var result = await mainService.RemoveWaitingDigest(new(key));
        if (result.IsFailed)
        {
            logger.LogError("Failed to remove waiting digest: {Errors}", result.Errors);
            return Result.Fail(result.Errors);
        }
        return Result.Ok();
    }

    public Task<List<RssProvider>> GetRssProviders(CancellationToken ct)
    {
        // Mock implementation - in real app this would come from backend configuration
        var providers = new List<RssProvider>
        {
            new()
            {
                Id = "rsshub",
                Name = "RSSHub (t.me)",
                BaseUrl = "https://rsshub.app/telegram/channel/",
            },
            new()
            {
                Id = "rssbridge",
                Name = "RSS Bridge",
                BaseUrl =
                    "https://rssbridge.org/bridge01/?action=display&bridge=Telegram&format=Atom&username=",
            },
            new()
            {
                Id = "telegramrss",
                Name = "Telegram RSS Bot",
                BaseUrl = "https://t.me/rss_bot?channel=",
            },
        };

        return Task.FromResult(providers);
    }
}

public static class StepsHelper
{
    public static DigestStepViewModelEnum MapToVm(this DigestStepTypeModelEnum step) =>
        step switch
        {
            DigestStepTypeModelEnum.Queued => DigestStepViewModelEnum.Queued,
            DigestStepTypeModelEnum.ProcessingStarted => DigestStepViewModelEnum.ProcessingStarted,
            DigestStepTypeModelEnum.RssReadingStarted => DigestStepViewModelEnum.RssReadingStarted,
            DigestStepTypeModelEnum.RssReadingFinished =>
                DigestStepViewModelEnum.RssReadingFinished,
            DigestStepTypeModelEnum.AiProcessing => DigestStepViewModelEnum.AiProcessing,
            DigestStepTypeModelEnum.Success => DigestStepViewModelEnum.Success,
            DigestStepTypeModelEnum.Cancelled => DigestStepViewModelEnum.Cancelled,
            DigestStepTypeModelEnum.Error => DigestStepViewModelEnum.Error,
            DigestStepTypeModelEnum.NoPostsFound => DigestStepViewModelEnum.NoPostsFound,
            _ => throw new UnreachableException($"Invalid {nameof(DigestStepTypeModelEnum)}"),
        };

    public static string FormatErrorStep(this ErrorStepModel errorStep) =>
        (errorStep.Message is { } m ? m + "\n\n" : "")
        + (errorStep.Exception is { } ex ? ex + "\n\n" : "")
        + (errorStep.Errors is { } er ? string.Join("\n\ncaused  by: ", er) : "");

    public static bool IsFinished(this DigestStepViewModelEnum step)
    {
        return step
            is DigestStepViewModelEnum.Success
                or DigestStepViewModelEnum.Error
                or DigestStepViewModelEnum.Cancelled
                or DigestStepViewModelEnum.NoPostsFound;
    }

    public static AiProcessingStepModel? GetLastAiStepOrDefault(this IDigestStepModel[] steps)
    {
        return steps.LastOrDefault(x => x is AiProcessingStepModel) as AiProcessingStepModel;
    }
}
