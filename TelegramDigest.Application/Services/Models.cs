namespace TelegramDigest.Application.Services;

public record ChannelModel(
    ChannelId ChannelId,
    string Description,
    string Name,
    Uri ImageUrl
);

public record PostModel(
    PostId PostId,
    ChannelId ChannelId,
    string Title,
    string Description,
    Uri Url,
    DateTime PublishedAt
);

public record PostSummaryModel(
    PostId PostId,
    ChannelId ChannelId,
    string Summary,
    Uri Url,
    DateTime PublishedAt,
    ImportanceModel Importance
);

public record DigestModel(
    DigestId DigestId,
    List<PostSummaryModel> Posts,
    DigestSummaryModel DigestSummary
);

public record DigestSummaryModel(
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

public record SettingsModel(
    string EmailRecipient,
    TimeOnly DigestTime,
    SmtpSettingsModel SmtpSettings,
    OpenAiSettingsModel OpenAiSettings
);

public record SmtpSettingsModel(
    string Host,
    int Port,
    string Username,
    string Password,
    bool UseSsl
);

public record OpenAiSettingsModel(
    string ApiKey,
    string Model,
    int MaxTokens
);

//TODO: verify value on creation
public record ImportanceModel(int Value);
