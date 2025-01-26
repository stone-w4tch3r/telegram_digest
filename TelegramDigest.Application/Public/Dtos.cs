namespace TelegramDigest.Application.Public;

public record ChannelDto(string ChannelName, string Description, string Name, string ImageUrl);

public record DigestSummaryDto(
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
    string Url,
    string ChannelName,
    string Summary,
    DateTime PublishedAt,
    int Importance
);

public record DigestDto(Guid DigestId, List<PostSummaryDto> Posts, DigestSummaryDto Summary);

public record SmtpSettingsDto(string Host, int Port, string Username, string Password, bool UseSsl);

public record OpenAiSettingsDto(string ApiKey, string Model, int MaxTokens);

public record SettingsDto(
    string EmailRecipient,
    TimeOnly DigestTime,
    SmtpSettingsDto SmtpSettings,
    OpenAiSettingsDto OpenAiSettings
);
