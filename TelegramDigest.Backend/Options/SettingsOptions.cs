using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TelegramDigest.Backend.Options;

internal sealed record SettingsJson(
    [property: JsonRequired] string EmailRecipient,
    [property: JsonRequired] TimeOnly DigestTime,
    [property: JsonRequired] SmtpSettingsJson SmtpSettings,
    [property: JsonRequired] OpenAiSettingsJson OpenAiSettings,
    [property: JsonRequired] PromptSettingsJson PromptSettings
);

internal sealed record PromptSettingsJson(
    [property: JsonRequired] string PostSummaryUserPrompt,
    [property: JsonRequired] string PostImportanceUserPrompt,
    [property: JsonRequired] string DigestSummaryUserPrompt
);

internal sealed record SmtpSettingsJson(
    [property: JsonRequired] string Host,
    [property: JsonRequired] int Port,
    [property: JsonRequired] string Username,
    [property: JsonRequired] string Password,
    [property: JsonRequired] bool UseSsl
);

internal sealed record OpenAiSettingsJson(
    [property: JsonRequired] string ApiKey,
    [property: JsonRequired] string Model,
    [property: JsonRequired] int MaxTokens,
    [property: JsonRequired] Uri Endpoint
);

internal class SettingsOptions
{
    [Required(ErrorMessage = "DEFAULT_SETTINGS configuration option was not set")]
    [JsonString(DisplayName = "DEFAULT_SETTINGS")]
    [ConfigurationKeyName("DEFAULT_SETTINGS")]
    public required string DefaultSettings { get; init; }
}
