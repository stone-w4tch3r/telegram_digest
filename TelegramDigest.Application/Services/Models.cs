using JetBrains.Annotations;

namespace TelegramDigest.Application.Services;

internal record ChannelModel(ChannelId ChannelId, string Description, string Name, Uri ImageUrl);

internal record PostModel(ChannelId ChannelId, Html HtmlContent, Uri Url, DateTime PublishedAt);

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

/// <summary>
/// Describes the importance of a post. Importance value must be between 1 and 10, inclusive
/// </summary>
internal record ImportanceModel
{
    internal ImportanceModel(int Value)
    {
        this.Value = Value is > 0 and <= 10
            ? Value
            : throw new ArgumentOutOfRangeException(
                nameof(Value),
                "Importance value must be between 1 and 10, inclusive"
            );
    }

    internal int Value { get; }
}
