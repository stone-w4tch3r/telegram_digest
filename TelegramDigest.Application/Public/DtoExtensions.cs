using TelegramDigest.Application.Services;

namespace TelegramDigest.Application.Public;

/// <summary>
/// Extension methods for converting between DTOs and domain models
/// </summary>
internal static class DtoExtensions
{
    internal static ChannelDto ToDto(this ChannelModel model) =>
        new(
            ChannelName: model.TgId.ChannelName,
            Description: model.Description,
            Name: model.Title,
            ImageUrl: model.ImageUrl.ToString()
        );

    internal static DigestSummaryDto ToDto(this DigestSummaryModel model) =>
        new(
            DigestId: model.DigestId.Id,
            Title: model.Title,
            Summary: model.PostsSummary,
            PostsCount: model.PostsCount,
            AverageImportance: model.AverageImportance,
            CreatedAt: model.CreatedAt,
            DateFrom: model.DateFrom,
            DateTo: model.DateTo
        );

    internal static DigestDto ToDto(this DigestModel model) =>
        new(
            DigestId: model.DigestId.Id,
            Posts: model.PostsSummaries.Select(p => p.ToDto()).ToList(),
            Summary: model.DigestSummary.ToDto()
        );

    internal static PostSummaryDto ToDto(this PostSummaryModel model) =>
        new(
            ChannelName: model.ChannelTgId.ChannelName,
            Summary: model.Summary,
            Url: model.Url.ToString(),
            PublishedAt: model.PublishedAt,
            Importance: model.Importance.Value
        );

    internal static SettingsModel ToDomain(this SettingsDto dto) =>
        new(
            EmailRecipient: dto.EmailRecipient,
            DigestTime: new(dto.DigestTimeUtc),
            SmtpSettings: new(
                Host: dto.SmtpSettings.Host,
                Port: dto.SmtpSettings.Port,
                Username: dto.SmtpSettings.Username,
                Password: dto.SmtpSettings.Password,
                UseSsl: dto.SmtpSettings.UseSsl
            ),
            OpenAiSettings: new(
                ApiKey: dto.OpenAiSettings.ApiKey,
                Model: dto.OpenAiSettings.Model,
                MaxTokens: dto.OpenAiSettings.MaxTokens
            )
        );

    internal static SettingsDto ToDto(this SettingsModel settings)
    {
        return new(
            EmailRecipient: settings.EmailRecipient,
            DigestTimeUtc: settings.DigestTime.Time,
            SmtpSettings: new(
                Host: settings.SmtpSettings.Host,
                Port: settings.SmtpSettings.Port,
                Username: settings.SmtpSettings.Username,
                Password: settings.SmtpSettings.Password,
                UseSsl: settings.SmtpSettings.UseSsl
            ),
            OpenAiSettings: new(
                ApiKey: settings.OpenAiSettings.ApiKey,
                Model: settings.OpenAiSettings.Model,
                MaxTokens: settings.OpenAiSettings.MaxTokens
            )
        );
    }
}
