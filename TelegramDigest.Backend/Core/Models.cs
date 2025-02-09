using TelegramDigest.HostHandler;

namespace TelegramDigest.Backend.Core;

public record ChannelModel(ChannelTgId TgId, string Description, string Title, Uri ImageUrl);

public record PostModel(ChannelTgId ChannelTgId, Html HtmlContent, Uri Url, DateTime PublishedAt);

public record PostSummaryModel(
    ChannelTgId ChannelTgId,
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    Importance Importance
);

public record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> PostsSummaries,
    DigestSummaryModel DigestSummary
);

public record DigestSummaryModel(
    DigestId DigestId,
    string Title,
    string PostsSummary,
    int PostsCount,
    double AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo
);

public record SettingsModel(
    string EmailRecipient,
    TimeUtc DigestTime,
    SmtpSettingsModel SmtpSettings,
    OpenAiSettingsModel OpenAiSettings,
    PromptSettingsModel PromptSettings
);

public record SmtpSettingsModel(Host Host, int Port, string Username, string Password, bool UseSsl);

public record OpenAiSettingsModel(string ApiKey, string Model, int MaxTokens, Uri Endpoint);

public record PromptSettingsModel(
    string PostSummarySystemPrompt,
    TemplateWithPost PostSummaryUserPrompt,
    string PostImportanceSystemPrompt,
    TemplateWithPost PostImportanceUserPrompt,
    string DigestSummarySystemPrompt,
    TemplateWithDigest DigestSummaryUserPrompt
);
