using System.Diagnostics;
using System.ServiceModel.Syndication;
using TelegramDigest.Backend.Core;
using TelegramDigest.Web.Models.ViewModels;

namespace TelegramDigest.Web.Services;

public sealed class BackendClient(IMainService mainService, ILogger<BackendClient> logger)
{
    public async Task<List<DigestSummaryViewModel>> GetDigestSummaries()
    {
        var result = await mainService.GetDigestSummaries();
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
        var result = await mainService.GetDigest(new(id));
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
                    ChannelName = p.ChannelTgId.ChannelName,
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
        var result = await mainService.DeleteDigest(new(id));
        if (result.IsFailed)
        {
            logger.LogError("Failed to delete digest {DigestId}: {Errors}", id, result.Errors);
            throw new($"Failed to delete digest {id}");
        }
    }

    public async Task<Guid> QueueDigest()
    {
        var digestId = Guid.NewGuid();
        var digestResult = await mainService.QueueDigestForLastPeriod(new(digestId));
        if (digestResult.IsFailed)
        {
            logger.LogError("Failed to generate digest: {Errors}", digestResult.Errors);
            throw new("Failed to generate digest");
        }

        return digestId;
    }

    public async Task<List<ChannelViewModel>> GetChannels()
    {
        var result = await mainService.GetChannels();
        if (result.IsFailed)
        {
            logger.LogError("Failed to get channels: {Errors}", result.Errors);
            throw new("Failed to get channels");
        }

        return result
            .Value.Select(c => new ChannelViewModel { TgId = c.TgId, Title = c.Title })
            .ToList();
    }

    public async Task AddOrUpdateChannel(AddChannelViewModel channel)
    {
        var result = await mainService.AddOrUpdateChannel(new(channel.TgId));
        if (result.IsFailed)
        {
            logger.LogError("Failed to add channel: {Errors}", result.Errors);
            throw new("Failed to add channel");
        }
    }

    public async Task DeleteChannelAsync(string tgId)
    {
        var result = await mainService.RemoveChannel(new(tgId));
        if (result.IsFailed)
        {
            logger.LogError("Failed to delete channel {ChannelId}: {Errors}", tgId, result.Errors);
            throw new($"Failed to delete channel {tgId}");
        }
    }

    public async Task<SettingsViewModel> GetSettings()
    {
        var result = await mainService.GetSettings();
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

        var result = await mainService.UpdateSettings(settingsModel);
        if (result.IsFailed)
        {
            logger.LogError("Failed to update settings: {Errors}", result.Errors);
            throw new("Failed to update settings");
        }
    }

    public async Task<SyndicationFeed> GetRssFeed()
    {
        var result = await mainService.GetRssFeed();
        if (result.IsFailed)
        {
            logger.LogError("Failed to get RSS feed: {Errors}", result.Errors);
            throw new("Failed to get RSS feed");
        }
        return result.Value;
    }

    public async Task<DigestProgressViewModel> GetDigestProgress(Guid id)
    {
        var result = await mainService.GetDigestSteps(new(id));
        if (result.IsFailed || result.Value.Length == 0)
        {
            logger.LogError("Failed to get digest statuses: {Errors}", result.Errors);
            throw new("Failed to get digest statuses");
        }

        var steps = result.Value.OrderBy(x => x.Timestamp).ToArray();
        return new()
        {
            Id = id,
            StartedAt = steps[0].Timestamp,
            CompletedAt = steps[^1].Type.MapToVm().IsFinished() ? steps[^1].Timestamp : null,
            CurrentStep = steps.Last().Type.MapToVm(),
            PercentComplete =
                steps[^1].Type.MapToVm().IsFinished() ? 100
                : steps[^1] is AiProcessingStepModel aiStatus ? aiStatus.Percentage
                : 1,
            Steps = steps
                .Select(x => new DigestStepViewModel
                {
                    Type = x.Type.MapToVm(),
                    Message = x.Message,
                    Timestamp = x.Timestamp,
                    Channels = x is RssReadingStartedStepModel readingStartedStep
                        ? readingStartedStep.Channels.Select(c => c.ChannelName).ToArray()
                        : null,
                    PostsCount = x is RssReadingFinishedStepModel readingFinishedStep
                        ? readingFinishedStep.PostsCount
                        : null,
                })
                .ToArray(),
            ErrorMessage = steps[^1] is ErrorStepModel error ? error.FormatErrorStep() : null,
            IsStepsOrderValid = steps.IsStepsOrderValid(),
        };
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

    public static bool IsStepsOrderValid(this IDigestStepModel[] steps)
    {
        for (var i = 1; i < steps.Length; i++)
        {
            if (GetStepWeight(steps[i - 1].Type) >= GetStepWeight(steps[i].Type))
            {
                return false;
            }
        }
        return true;
    }

    private static int GetStepWeight(DigestStepTypeModelEnum stepType) =>
        stepType switch
        {
            DigestStepTypeModelEnum.Queued => 0,
            DigestStepTypeModelEnum.ProcessingStarted => 1,
            DigestStepTypeModelEnum.RssReadingStarted => 2,
            DigestStepTypeModelEnum.RssReadingFinished => 3,
            DigestStepTypeModelEnum.NoPostsFound => 3,
            DigestStepTypeModelEnum.AiProcessing => 5,
            DigestStepTypeModelEnum.Cancelled => 100,
            DigestStepTypeModelEnum.Error => 101,
            DigestStepTypeModelEnum.Success => 1000,
            _ => throw new UnreachableException($"Invalid {nameof(DigestStepTypeModelEnum)}"),
        };
}
