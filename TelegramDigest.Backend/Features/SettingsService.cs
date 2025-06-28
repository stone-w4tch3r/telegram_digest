using FluentResults;
using TelegramDigest.Backend.Db;
using TelegramDigest.Backend.Models;

namespace TelegramDigest.Backend.Features;

internal interface ISettingsService
{
    /// <summary>
    /// Loads application settings.
    /// </summary>
    public Task<Result<SettingsModel>> LoadSettings(CancellationToken ct);

    /// <summary>
    /// Saves application settings.
    /// </summary>
    public Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct);
}

/// <summary>
/// Manages application settings with database persistence.
/// </summary>
internal sealed class SettingsService(
    ISettingsRepository repository,
    ILogger<SettingsService> logger
) : ISettingsService
{
    public async Task<Result<SettingsModel>> LoadSettings(CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Attempting to load settings from database");
            var result = await repository.LoadSettings(ct);
            if (result.IsSuccess && result.Value is not null)
            {
                return Result.Ok(result.Value);
            }

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "No settings found in database. Creating and saving default settings."
                );
                var defaultSettings = CreateDefaultSettings();
                var saveResult = await repository.SaveSettings(defaultSettings, ct);
                if (!saveResult.IsSuccess)
                {
                    logger.LogError(
                        "Failed to save default settings to database: {Error}",
                        saveResult.Errors
                    );
                    return Result.Fail(saveResult.Errors);
                }

                return Result.Ok(defaultSettings);
            }

            logger.LogError("Failed to load settings from database: {Error}", result.Errors);
            return Result.Fail(result.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while loading settings from database");
            return Result.Fail(
                new Error("Exception occurred while loading settings from database").CausedBy(ex)
            );
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct)
    {
        try
        {
            var result = await repository.SaveSettings(settings, ct);
            if (result.IsSuccess)
            {
                return Result.Ok();
            }

            logger.LogError("Failed to save settings to database: {Error}", result.Errors);
            return Result.Fail(
                new Error("Failed to save settings to database").CausedBy(result.Errors)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while saving settings to database");
            return Result.Fail(
                new Error("Exception occurred while saving settings to database").CausedBy(ex)
            );
        }
    }

    private static SettingsModel CreateDefaultSettings() =>
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
}
