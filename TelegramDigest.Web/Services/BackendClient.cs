using TelegramDigest.Backend.Core;
using TelegramDigest.Web.Models.ViewModels;

namespace TelegramDigest.Web.Services;

public class BackendClient(IMainService mainService, ILogger<BackendClient> logger)
{
    public async Task<List<DigestSummaryViewModel>> GetDigestsAsync()
    {
        try
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
                    Id = d.DigestId.Id,
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting digests");
            throw;
        }
    }

    public async Task<(
        DigestSummaryViewModel summary,
        PostSummaryViewModel[] posts
    )?> GetDigestAsync(Guid id)
    {
        try
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
                    Id = result.Value.DigestId.Id,
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
                        Importance = p.Importance.Value,
                    })
                    .ToArray()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting digest {DigestId}", id);
            throw;
        }
    }

    public async Task GenerateDigestAsync()
    {
        try
        {
            var result = await mainService.ProcessDailyDigest();
            if (result.IsFailed)
            {
                logger.LogError("Failed to generate digest: {Errors}", result.Errors);
                throw new("Failed to generate digest");
            }

            //TODO show generated digest Id
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating digest");
            throw;
        }
    }

    public async Task<List<ChannelViewModel>> GetChannelsAsync()
    {
        try
        {
            var result = await mainService.GetChannels();
            if (result.IsFailed)
            {
                logger.LogError("Failed to get channels: {Errors}", result.Errors);
                throw new("Failed to get channels");
            }

            return result
                .Value.Select(c => new ChannelViewModel
                {
                    TgId = c.TgId.ChannelName,
                    Description = c.Description,
                    Title = c.Title,
                    ImageUrl = c.ImageUrl.ToString(),
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting channels");
            throw;
        }
    }

    public async Task AddChannelAsync(AddChannelViewModel channel)
    {
        try
        {
            var result = await mainService.AddChannel(new(channel.TgId));
            if (result.IsFailed)
            {
                logger.LogError("Failed to add channel: {Errors}", result.Errors);
                throw new("Failed to add channel");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding channel");
            throw;
        }
    }

    public async Task DeleteChannelAsync(string tgId)
    {
        try
        {
            var result = await mainService.RemoveChannel(new(tgId));
            if (result.IsFailed)
            {
                logger.LogError(
                    "Failed to delete channel {ChannelId}: {Errors}",
                    tgId,
                    result.Errors
                );
                throw new($"Failed to delete channel {tgId}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting channel {ChannelId}", tgId);
            throw;
        }
    }

    public async Task<SettingsViewModel> GetSettingsAsync()
    {
        try
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
                SmtpHost = settings.SmtpSettings.Host.HostPart,
                SmtpPort = settings.SmtpSettings.Port,
                SmtpUsername = settings.SmtpSettings.Username,
                SmtpPassword = settings.SmtpSettings.Password,
                SmtpUseSsl = settings.SmtpSettings.UseSsl,
                OpenAiApiKey = settings.OpenAiSettings.ApiKey,
                OpenAiModel = settings.OpenAiSettings.Model,
                OpenAiMaxToken = settings.OpenAiSettings.MaxTokens,
                OpenAiEndpoint = settings.OpenAiSettings.Endpoint,
                DigestTimeUtc = settings.DigestTime.Time,
                PromptSystemDigestSummary = settings.PromptSettings.DigestSummarySystemPrompt,
                PromptUserDigestSummary = settings.PromptSettings.DigestSummaryUserPrompt,
                PromptSystemPostSummary = settings.PromptSettings.PostSummarySystemPrompt,
                PromptUserPostSummary = settings.PromptSettings.PostSummaryUserPrompt,
                PromptSystemPostImportance = settings.PromptSettings.PostImportanceSystemPrompt,
                PromptUserPostImportance = settings.PromptSettings.PostImportanceUserPrompt,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting settings");
            throw;
        }
    }

    public async Task UpdateSettingsAsync(SettingsViewModel settings)
    {
        try
        {
            var settingsModel = new SettingsModel(
                settings.RecipientEmail,
                new(settings.DigestTimeUtc),
                new(
                    new(settings.SmtpHost),
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
                    PostSummarySystemPrompt: settings.PromptSystemPostSummary,
                    PostSummaryUserPrompt: new(settings.PromptUserPostSummary),
                    PostImportanceSystemPrompt: settings.PromptSystemPostImportance,
                    PostImportanceUserPrompt: new(settings.PromptUserPostImportance),
                    DigestSummarySystemPrompt: settings.PromptSystemDigestSummary,
                    DigestSummaryUserPrompt: new(settings.PromptUserDigestSummary)
                )
            );

            var result = await mainService.UpdateSettings(settingsModel);
            if (result.IsFailed)
            {
                logger.LogError("Failed to update settings: {Errors}", result.Errors);
                throw new("Failed to update settings");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating settings");
            throw;
        }
    }
}
