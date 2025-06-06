using System.Diagnostics;
using System.ServiceModel.Syndication;
using TelegramDigest.Backend.Core;
using TelegramDigest.Web.Models.ViewModels;

namespace TelegramDigest.Web.Services;

public sealed class BackendClient(IMainService mainService, ILogger<BackendClient> logger)
{
    public async Task<List<DigestSummaryViewModel>> GetDigestSummaries()
    {
        var result = await mainService.GetDigestSummaries(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digests: {Errors}", result.Errors);
            throw new("Failed to get digests");
        }

        return result
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
            .ToList();
    }

    public async Task<(DigestSummaryViewModel summary, PostSummaryViewModel[] posts)?> GetDigest(
        Guid id
    )
    {
        var result = await mainService.GetDigest(new(id), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digest {DigestId}: {Errors}", id, result.Errors);
            throw new($"Failed to get digest {id}");
        }

        if (result.Value is null)
        {
            return null;
        }

        return (
            new()
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
        );
    }

    public async Task DeleteDigest(Guid id)
    {
        var result = await mainService.DeleteDigest(new(id), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to delete digest {DigestId}: {Errors}", id, result.Errors);
            throw new($"Failed to delete digest {id}");
        }
    }

    public async Task<Guid> QueueDigest(DigestGenerationViewModel model)
    {
        var digestId = Guid.NewGuid();
        var filter = new DigestFilterModel(
            DateFrom: DateOnly.FromDateTime(model.DateFrom),
            DateTo: DateOnly.FromDateTime(model.DateTo),
            SelectedFeeds: model.SelectedFeeds.Select(f => new FeedUrl(f)).ToHashSet()
        );

        var digestResult = await mainService.QueueDigest(
            new(digestId),
            filter,
            CancellationToken.None
        );

        if (digestResult.IsFailed)
        {
            logger.LogError("Failed to generate digest: {Errors}", digestResult.Errors);
            throw new("Failed to generate digest");
        }

        return digestId;
    }

    public async Task<List<FeedViewModel>> GetFeeds()
    {
        var result = await mainService.GetFeeds(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get feeds: {Errors}", result.Errors);
            throw new("Failed to get feeds");
        }

        return result
            .Value.Select(f => new FeedViewModel { Title = f.Title, Url = f.FeedUrl.ToString() })
            .ToList();
    }

    public async Task AddOrUpdateFeed(AddFeedViewModel feed)
    {
        var result = await mainService.AddOrUpdateFeed(new(feed.FeedUrl), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to add feed: {Errors}", result.Errors);
            throw new("Failed to add feed");
        }
    }

    public async Task DeleteFeedAsync(string feedUrl)
    {
        var result = await mainService.RemoveFeed(new(feedUrl), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to delete feed {FeedUrl}: {Errors}", feedUrl, result.Errors);
            throw new($"Failed to delete feed {feedUrl}");
        }
    }

    public async Task<SettingsViewModel> GetSettings()
    {
        var result = await mainService.GetSettings(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get settings: {Errors}", result.Errors);
            throw new("Failed to get settings");
        }

        var settings = result.Value;
        return new()
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
            PromptDigestSummarySystem = settings.PromptSettings.DigestSummarySystemPrompt,
            PromptDigestSummaryUser = settings.PromptSettings.DigestSummaryUserPrompt,
            PromptPostSummarySystem = settings.PromptSettings.PostSummarySystemPrompt,
            PromptPostSummaryUser = settings.PromptSettings.PostSummaryUserPrompt,
            PromptPostImportanceSystem = settings.PromptSettings.PostImportanceSystemPrompt,
            PromptPostImportanceUser = settings.PromptSettings.PostImportanceUserPrompt,
        };
    }

    public async Task UpdateSettings(SettingsViewModel settings)
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
                PostSummarySystemPrompt: settings.PromptPostSummarySystem,
                PostSummaryUserPrompt: new(settings.PromptPostSummaryUser),
                PostImportanceSystemPrompt: settings.PromptPostImportanceSystem,
                PostImportanceUserPrompt: new(settings.PromptPostImportanceUser),
                DigestSummarySystemPrompt: settings.PromptDigestSummarySystem,
                DigestSummaryUserPrompt: new(settings.PromptDigestSummaryUser)
            )
        );

        var result = await mainService.UpdateSettings(settingsModel, CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to update settings: {Errors}", result.Errors);
            throw new("Failed to update settings");
        }
    }

    public async Task<SyndicationFeed> GetRssFeed()
    {
        var result = await mainService.GetRssFeed(CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get RSS feed: {Errors}", result.Errors);
            throw new("Failed to get RSS feed");
        }

        return result.Value;
    }

    public async Task<DigestProgressViewModel> GetDigestProgress(Guid id)
    {
        var result = await mainService.GetDigestSteps(new(id), CancellationToken.None);
        if (result.IsFailed)
        {
            logger.LogError("Failed to get digest statuses: {Errors}", result.Errors);
            throw new("Failed to get digest statuses");
        }

        var steps = result.Value.OrderBy(x => x.Timestamp).ToArray();
        return steps.Length == 0
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
            : new()
            {
                Id = id,
                StartedAt = steps[0].Timestamp,
                CompletedAt = steps[^1].Type.MapToVm().IsFinished() ? steps[^1].Timestamp : null,
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
                ErrorMessage = steps[^1] is ErrorStepModel error ? error.FormatErrorStep() : null,
            };
    }

    public async Task CancelDigest(Guid digestId)
    {
        var result = await mainService.CancelDigest(new(digestId));
        if (result.IsFailed)
        {
            logger.LogError("Failed to cancel digest: {Errors}", result.Errors);
            throw new("Failed to cancel digest");
        }
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

    public async Task RemoveWaitingDigest(Guid key)
    {
        var result = await mainService.RemoveWaitingDigest(new(key));
        if (result.IsFailed)
        {
            logger.LogError("Failed to remove waiting digest: {Errors}", result.Errors);
            throw new("Failed to remove waiting digest");
        }
    }

    public Task<List<RssProvider>> GetRssProviders()
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
