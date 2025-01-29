using JetBrains.Annotations;

namespace TelegramDigest.Application.Services;

internal record ChannelModel(ChannelTgId TgId, string Description, string Title, Uri ImageUrl);

internal record PostModel(ChannelTgId ChannelTgId, Html HtmlContent, Uri Url, DateTime PublishedAt);

internal record PostSummaryModel(
    ChannelTgId ChannelTgId,
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    Importance Importance
);

internal record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> PostsSummaries,
    DigestSummaryModel DigestSummary
);

internal record DigestSummaryModel(
    DigestId DigestId,
    string Title,
    string PostsSummary,
    int PostsCount,
    double AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo,
    Uri ImageUrl
);

internal record SettingsModel(
    string EmailRecipient,
    TimeUtc DigestTime,
    SmtpSettingsModel SmtpSettings,
    OpenAiSettingsModel OpenAiSettings
);

internal record SmtpSettingsModel(
    string Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl
);

internal record OpenAiSettingsModel(string ApiKey, string Model, int MaxTokens);
