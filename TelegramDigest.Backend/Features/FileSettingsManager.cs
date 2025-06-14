using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using Microsoft.Extensions.Options;
using TelegramDigest.Backend.Models;
using TelegramDigest.Backend.Options;

namespace TelegramDigest.Backend.Features;

internal interface ISettingsManager
{
    /// <summary>
    /// Loads application settings from the file system.
    /// </summary>
    /// <returns>A result containing the loaded settings or an error.</returns>
    public Task<Result<SettingsModel>> LoadSettings(CancellationToken ct);

    /// <summary>
    /// Saves application settings to the file system.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct);
}

/// <summary>
/// Manages application settings with file-based persistence
/// </summary>
internal sealed class FileSettingsManager(
    IOptions<BackendDeploymentOptions> options,
    ILogger<FileSettingsManager> logger
) : ISettingsManager
{
    private readonly FileInfo _settingsFileInfo = new(options.Value.SettingsFilePath);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    public async Task<Result<SettingsModel>> LoadSettings(CancellationToken ct)
    {
        try
        {
            _settingsFileInfo.Refresh();
            if (!_settingsFileInfo.Exists)
            {
                logger.LogInformation("No settings file found.");
                var emptySettings = CreateEmptySettings();
                await SaveSettings(emptySettings, ct);
                return Result.Ok(emptySettings);
            }

            var jsonStr = await File.ReadAllTextAsync(_settingsFileInfo.FullName, ct);
            var settingsJsonResult = Result.Try(
                () => JsonSerializer.Deserialize<SettingsJson>(jsonStr, _jsonOptions)
            );
            if (!settingsJsonResult.IsSuccess)
            {
                logger.LogError(
                    "Failed to deserialize settings JSON: {Error}",
                    settingsJsonResult.Errors
                );
                return Result.Fail(
                    [new Error("Failed to deserialize settings JSON"), .. settingsJsonResult.Errors]
                );
            }
            if (settingsJsonResult.Value is null)
            {
                logger.LogError("Failed to deserialize settings JSON, deserializer returned null");
                return Result.Fail(
                    "Failed to deserialize settings JSON, deserializer returned null"
                );
            }

            var settingsModelResult = Result.Try(() => MapFromJson(settingsJsonResult.Value));
            if (!settingsModelResult.IsSuccess)
            {
                logger.LogError(
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
            logger.LogError(ex, "Failed to load settings from {Path}", _settingsFileInfo);
            return Result.Fail(
                new Error($"Failed to load settings from {_settingsFileInfo}").CausedBy(ex)
            );
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFileInfo.FullName);
            if (directory is null)
            {
                logger.LogError(
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
                logger.LogError("Failed to serialize settings: {Error}", settingsJsonResult.Errors);
                return Result.Fail(
                    [new Error("Failed to serialize settings"), .. settingsJsonResult.Errors]
                );
            }

            var json = JsonSerializer.Serialize(settingsJsonResult.Value, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFileInfo.FullName, json, ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save settings to {Path}", _settingsFileInfo);
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
                new("Summarize the following post in one sentence:\n\n{Content}"),
                new(
                    "Please rate the importance of the following post on a scale of 1 to 10, where 1 is least important and 10 is most important.\n\n{Content}"
                ),
                new("Summarize the digest in one sentence:\n\n{Content}")
            )
        );

    private sealed record SettingsJson(
        [property: JsonRequired] string EmailRecipient,
        [property: JsonRequired] TimeOnly DigestTime,
        [property: JsonRequired] SmtpSettingsJson SmtpSettings,
        [property: JsonRequired] OpenAiSettingsJson OpenAiSettings,
        [property: JsonRequired] PromptSettingsJson PromptSettings
    );

    private sealed record PromptSettingsJson(
        [property: JsonRequired] string PostSummaryUserPrompt,
        [property: JsonRequired] string PostImportanceUserPrompt,
        [property: JsonRequired] string DigestSummaryUserPrompt
    );

    private sealed record SmtpSettingsJson(
        [property: JsonRequired] string Host,
        [property: JsonRequired] int Port,
        [property: JsonRequired] string Username,
        [property: JsonRequired] string Password,
        [property: JsonRequired] bool UseSsl
    );

    private sealed record OpenAiSettingsJson(
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
                settings.PromptSettings.PostSummaryUserPrompt.Text,
                settings.PromptSettings.PostImportanceUserPrompt.Text,
                settings.PromptSettings.DigestSummaryUserPrompt.Text
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
                new(settingsJson.PromptSettings.PostSummaryUserPrompt),
                new(settingsJson.PromptSettings.PostImportanceUserPrompt),
                new(settingsJson.PromptSettings.DigestSummaryUserPrompt)
            )
        );
}
