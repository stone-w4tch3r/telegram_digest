using TelegramDigest.Types.Host;

namespace TelegramDigest.Backend.Core;

public sealed record ChannelModel(ChannelTgId TgId, string Description, string Title, Uri ImageUrl);

public sealed record PostModel(
    ChannelTgId ChannelTgId, // TODO migrate this to 
    Html HtmlContent,
    Uri Url,
    DateTime PublishedAt
);

public sealed record PostSummaryModel(
    ChannelTgId ChannelTgId, // TODO migrate this to FeedUrl
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    Importance Importance
);

public sealed record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> PostsSummaries,
    DigestSummaryModel DigestSummary
);

public sealed record DigestSummaryModel(
    DigestId DigestId,
    string Title,
    string PostsSummary,
    int PostsCount,
    double AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo
);

public record DigestFilterModel(
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlySet<ChannelTgId>? SelectedChannels = null
);

public enum DigestGenerationResultModelEnum
{
    Success,
    NoPosts,
}

public sealed record SettingsModel(
    string EmailRecipient,
    TimeUtc DigestTime,
    SmtpSettingsModel SmtpSettings,
    OpenAiSettingsModel OpenAiSettings,
    PromptSettingsModel PromptSettings
);

public sealed record SmtpSettingsModel(
    Host Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl
);

public sealed record OpenAiSettingsModel(string ApiKey, string Model, int MaxTokens, Uri Endpoint);

public sealed record PromptSettingsModel(
    string PostSummarySystemPrompt,
    TemplateWithContent PostSummaryUserPrompt,
    string PostImportanceSystemPrompt,
    TemplateWithContent PostImportanceUserPrompt,
    string DigestSummarySystemPrompt,
    TemplateWithContent DigestSummaryUserPrompt
);
