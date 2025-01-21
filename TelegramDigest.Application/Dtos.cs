namespace TelegramDigest.Application;

public record ChannelDto(
    string ChannelName,
    string Description,
    string Name,
    string ImageUrl
);

public record DigestPreviewDto(
    Guid DigestId,
    string Title,
    string Summary,
    int PostsCount,
    int AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo,
    string ImageUrl
);

public record PostSummaryDto(
    Guid PostId,
    string ChannelName,
    string Summary,
    string Url,
    DateTime PublishedAt,
    int Importance
);

public record DigestDto(
    Guid DigestId,
    List<PostSummaryDto> Posts,
    DigestPreviewDto Summary
);

public record SmtpSettingsDto(
    string Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl
);

public record OpenAiSettingsDto(
    string ApiKey,
    string Model,
    int MaxTokens
);

public record SettingsDto(
    string EmailRecipient,
    TimeOnly DigestTime,
    SmtpSettingsDto SmtpSettings,
    OpenAiSettingsDto OpenAiSettings
);

/// <summary>
/// Extension methods for converting between DTOs and domain models
/// </summary>
public static class DtoExtensions
{
    public static ChannelDto ToDto(this ChannelModel model) =>
        new(
            ChannelName: model.ChannelId.Value,
            Description: model.Description,
            Name: model.Name,
            ImageUrl: model.ImageUrl.ToString()
        );

    public static DigestPreviewDto ToDto(this DigestSummaryModel model) =>
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

    public static DigestDto ToDto(this DigestModel model) =>
        new(
            DigestId: model.DigestId.Value,
            Posts: model.Posts.Select(p => p.ToDto()).ToList(),
            Summary: model.DigestSummary.ToDto()
        );

    public static PostSummaryDto ToDto(this PostSummaryModel model) =>
        new(
            PostId: model.PostId.Value,
            ChannelName: model.ChannelId.Value,
            Summary: model.Summary,
            Url: model.Url.ToString(),
            PublishedAt: model.PublishedAt,
            Importance: model.Importance.Value
        );

    public static SettingsModel ToDomain(this SettingsDto dto) =>
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
