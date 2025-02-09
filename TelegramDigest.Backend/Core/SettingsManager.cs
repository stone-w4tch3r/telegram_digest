using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;

namespace TelegramDigest.Backend.Core;

internal interface ISettingsManager
{
    /// <summary>
    /// Loads application settings from the file system.
    /// </summary>
    /// <returns>A result containing the loaded settings or an error.</returns>
    public Task<Result<SettingsModel>> LoadSettings();

    /// <summary>
    /// Saves application settings to the file system.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> SaveSettings(SettingsModel settings);
}

internal sealed class SettingsManager : ISettingsManager
{
    private const string SETTINGS_PATH = "runtime/settings.json";
    private readonly FileInfo _settingsFileInfo;
    private readonly ILogger<SettingsManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    /// <summary>
    /// Manages application settings with file-based persistence
    /// </summary>
    internal SettingsManager(string? settingsPath, ILogger<SettingsManager> logger)
    {
        _settingsFileInfo = new(settingsPath ?? SETTINGS_PATH);
        _logger = logger;
    }

    public async Task<Result<SettingsModel>> LoadSettings()
    {
        try
        {
            _settingsFileInfo.Refresh();
            if (!_settingsFileInfo.Exists)
            {
                _logger.LogInformation("No settings file found.");
                var emptySettings = CreateEmptySettings();
                await SaveSettings(emptySettings);
                return Result.Ok(emptySettings);
            }

            var jsonStr = await File.ReadAllTextAsync(_settingsFileInfo.FullName);
            var settingsJsonResult = Result.Try(
                () => JsonSerializer.Deserialize<SettingsJson>(jsonStr, _jsonOptions)
            );
            if (!settingsJsonResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to deserialize settings JSON: {Error}",
                    settingsJsonResult.Errors
                );
                return Result.Fail(
                    [new Error("Failed to deserialize settings JSON"), .. settingsJsonResult.Errors]
                );
            }
            if (settingsJsonResult.Value is null)
            {
                _logger.LogError("Failed to deserialize settings JSON, deserializer returned null");
                return Result.Fail(
                    "Failed to deserialize settings JSON, deserializer returned null"
                );
            }

            var settingsModelResult = Result.Try(() => MapFromJson(settingsJsonResult.Value));
            if (!settingsModelResult.IsSuccess)
            {
                _logger.LogError(
                    "Looks like settings in json are invalid: {Error}",
                    settingsModelResult.Errors
                );
                return Result.Fail(
                    [
                        new Error("Looks like settings in json are invalid"),
                        .. settingsModelResult.Errors,
                    ]
                );
            }

            return Result.Ok(settingsModelResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsFileInfo);
            return Result.Fail(
                new Error($"Failed to load settings from {_settingsFileInfo}").CausedBy(ex)
            );
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFileInfo.FullName);
            if (directory is null)
            {
                _logger.LogError(
                    "Invalid path to settings file [{Path}]",
                    _settingsFileInfo.FullName
                );
                return Result.Fail($"Invalid path to settings file [{_settingsFileInfo.FullName}]");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settingsJsonResult = Result.Try(() => MapToJson(settings));
            if (!settingsJsonResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to serialize settings: {Error}",
                    settingsJsonResult.Errors
                );
                return Result.Fail(
                    [new Error("Failed to serialize settings"), .. settingsJsonResult.Errors]
                );
            }

            var json = JsonSerializer.Serialize(settingsJsonResult.Value, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFileInfo.FullName, json);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsFileInfo);
            return Result.Fail(
                new Error($"Failed to save settings to {_settingsFileInfo}").CausedBy(ex)
            );
        }
    }

    private static SettingsModel CreateEmptySettings() =>
        new(
            "email@example.com",
            new(new(0, 0)),
            new(new("smtp.example.com"), 22, "username", "password", true),
            new("apikey", "model", 2048, new("https://generativelanguage.googleapis.com/v1beta/")),
            new(
                PostSummarySystemPrompt: "You are a summarizer of media posts. Use english language.",
                PostSummaryUserPrompt: new(
                    "Summarize the following post in one sentence:\n\n{Content}"
                ),
                PostImportanceSystemPrompt: "You are a post reviewer.",
                PostImportanceUserPrompt: new(
                    "Please rate the importance of the following post on a scale of 1 to 10, where 1 is least important and 10 is most important.\n\n{Content}"
                ),
                DigestSummarySystemPrompt: "You are a summarizer of big digests. Use english language.",
                DigestSummaryUserPrompt: new("Summarize the digest in one sentence:\n\n{Content}")
            )
        );

    private record SettingsJson(
        [property: JsonRequired] string EmailRecipient,
        [property: JsonRequired] TimeOnly DigestTime,
        [property: JsonRequired] SmtpSettingsJson SmtpSettings,
        [property: JsonRequired] OpenAiSettingsJson OpenAiSettings,
        [property: JsonRequired] PromptSettingsJson PromptSettings
    );

    private record PromptSettingsJson(
        [property: JsonRequired] string PostSummarySystemPrompt,
        [property: JsonRequired] string PostSummaryUserPrompt,
        [property: JsonRequired] string PostImportanceSystemPrompt,
        [property: JsonRequired] string PostImportanceUserPrompt,
        [property: JsonRequired] string DigestSummarySystemPrompt,
        [property: JsonRequired] string DigestSummaryUserPrompt
    );

    private record SmtpSettingsJson(
        [property: JsonRequired] string Host,
        [property: JsonRequired] int Port,
        [property: JsonRequired] string Username,
        [property: JsonRequired] string Password,
        [property: JsonRequired] bool UseSsl
    );

    private record OpenAiSettingsJson(
        [property: JsonRequired] string ApiKey,
        [property: JsonRequired] string Model,
        [property: JsonRequired] int MaxTokens,
        [property: JsonRequired] Uri Endpoint
    );

    private static SettingsJson MapToJson(SettingsModel settings) =>
        new(
            settings.EmailRecipient,
            settings.DigestTime.Time,
            new(
                settings.SmtpSettings.Host.ToString(),
                settings.SmtpSettings.Port,
                settings.SmtpSettings.Username,
                settings.SmtpSettings.Password,
                settings.SmtpSettings.UseSsl
            ),
            new(
                settings.OpenAiSettings.ApiKey,
                settings.OpenAiSettings.Model,
                settings.OpenAiSettings.MaxTokens,
                settings.OpenAiSettings.Endpoint
            ),
            new(
                settings.PromptSettings.PostSummarySystemPrompt,
                settings.PromptSettings.PostSummaryUserPrompt,
                settings.PromptSettings.PostImportanceSystemPrompt,
                settings.PromptSettings.PostImportanceUserPrompt,
                settings.PromptSettings.DigestSummarySystemPrompt,
                settings.PromptSettings.DigestSummaryUserPrompt
            )
        );

    private static SettingsModel MapFromJson(SettingsJson settingsJson) =>
        new(
            settingsJson.EmailRecipient,
            new(settingsJson.DigestTime),
            new(
                new(settingsJson.SmtpSettings.Host),
                settingsJson.SmtpSettings.Port,
                settingsJson.SmtpSettings.Username,
                settingsJson.SmtpSettings.Password,
                settingsJson.SmtpSettings.UseSsl
            ),
            new(
                settingsJson.OpenAiSettings.ApiKey,
                settingsJson.OpenAiSettings.Model,
                settingsJson.OpenAiSettings.MaxTokens,
                settingsJson.OpenAiSettings.Endpoint
            ),
            new(
                PostSummarySystemPrompt: settingsJson.PromptSettings.PostSummarySystemPrompt,
                PostSummaryUserPrompt: new(settingsJson.PromptSettings.PostSummaryUserPrompt),
                PostImportanceSystemPrompt: settingsJson.PromptSettings.PostImportanceSystemPrompt,
                PostImportanceUserPrompt: new(settingsJson.PromptSettings.PostImportanceUserPrompt),
                DigestSummarySystemPrompt: settingsJson.PromptSettings.DigestSummarySystemPrompt,
                DigestSummaryUserPrompt: new(settingsJson.PromptSettings.DigestSummaryUserPrompt)
            )
        );
}
