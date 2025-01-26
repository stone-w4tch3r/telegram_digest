namespace TelegramDigest.Application.Services;

internal record ChannelModel(ChannelId ChannelId, string Description, string Name, Uri ImageUrl);

internal record PostModel(
    ChannelId ChannelId,
    string Title,
    string Content,
    Uri Url,
    DateTime PublishedAt
);

internal record PostSummaryModel(
    ChannelId ChannelId,
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    ImportanceModel Importance
);

internal record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> Posts,
    DigestSummaryModel DigestSummary
);

internal record DigestSummaryModel(
    DigestId DigestId,
    string Title,
    string PostsSummary,
    int PostsCount,
    int AverageImportance,
    DateTime CreatedAt,
    DateTime DateFrom,
    DateTime DateTo,
    Uri ImageUrl
);

internal record SettingsModel(
    string EmailRecipient,
    TimeOnly DigestTime,
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

//TODO: verify value on creation
internal record ImportanceModel(int Value);
