using System.Text.Json.Serialization;

namespace TelegramDigest.API.Core;

public record ChannelDto(string ChannelName, string Description, string Name, string ImageUrl);

public record DigestSummaryDto(
    Guid DigestId,
    string Title,
    string Summary,
    int PostsCount,
    double AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo
);

public record PostSummaryDto(
    string Url,
    string ChannelName,
    string Summary,
    DateTime PublishedAt,
    int Importance
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DigestGenerationStatusDto
{
    Success,
    NoPosts,
}

public record DigestGenerationDto(Guid? GeneratedDigestId, DigestGenerationStatusDto Status);

public record DigestDto(Guid DigestId, List<PostSummaryDto> Posts, DigestSummaryDto Summary);

public record SmtpSettingsDto(string Host, int Port, string Username, string Password, bool UseSsl);

public record OpenAiSettingsDto(string ApiKey, string Model, int MaxTokens, string Endpoint);

public record PromptSettingsDto(
    string PostSummarySystemPrompt,
    string PostSummaryUserPrompt,
    string PostImportanceSystemPrompt,
    string PostImportanceUserPrompt,
    string DigestSummarySystemPrompt,
    string DigestSummaryUserPrompt
);

public record SettingsDto(
    string EmailRecipient,
    TimeOnly DigestTimeUtc,
    SmtpSettingsDto SmtpSettings,
    OpenAiSettingsDto OpenAiSettings,
    PromptSettingsDto PromptSettings
);
