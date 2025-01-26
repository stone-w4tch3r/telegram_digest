using TelegramDigest.Application.Services;

namespace TelegramDigest.Application.Public;

/// <summary>
/// Extension methods for converting between DTOs and domain models
/// </summary>
internal static class DtoExtensions
{
    internal static ChannelDto ToDto(this ChannelModel model) =>
        new(
            ChannelName: model.ChannelId.Value,
            Description: model.Description,
            Name: model.Name,
            ImageUrl: model.ImageUrl.ToString()
        );

    internal static DigestSummaryDto ToDto(this DigestSummaryModel model) =>
        new(
            DigestId: model.DigestId.Value,
            Title: model.Title,
            Summary: model.PostsSummary,
            PostsCount: model.PostsCount,
            AverageImportance: model.AverageImportance,
            CreatedAt: model.CreatedAt,
            DateFrom: model.DateFrom,
            DateTo: model.DateTo,
            ImageUrl: model.ImageUrl.ToString()
        );

    internal static DigestDto ToDto(this DigestModel model) =>
        new(
            DigestId: model.DigestId.Value,
            Posts: model.Posts.Select(p => p.ToDto()).ToList(),
            Summary: model.DigestSummary.ToDto()
        );

    internal static PostSummaryDto ToDto(this PostSummaryModel model) =>
        new(
            ChannelName: model.ChannelId.Value,
            Summary: model.Summary,
            Url: model.Url.ToString(),
            PublishedAt: model.PublishedAt,
            Importance: model.Importance.Value
        );

    internal static SettingsModel ToDomain(this SettingsDto dto) =>
        new(
            EmailRecipient: dto.EmailRecipient,
            DigestTime: dto.DigestTime,
            SmtpSettings: new SmtpSettingsModel(
                Host: dto.SmtpSettings.Host,
                Port: dto.SmtpSettings.Port,
                Username: dto.SmtpSettings.Username,
                Password: dto.SmtpSettings.Password,
                UseSsl: dto.SmtpSettings.UseSsl
            ),
            OpenAiSettings: new OpenAiSettingsModel(
                ApiKey: dto.OpenAiSettings.ApiKey,
                Model: dto.OpenAiSettings.Model,
                MaxTokens: dto.OpenAiSettings.MaxTokens
            )
        );
}
