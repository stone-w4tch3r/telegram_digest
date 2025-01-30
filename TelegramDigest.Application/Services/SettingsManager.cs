using System.Text.Json;
using FluentResults;

namespace TelegramDigest.Application.Services;

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
    private const string SettingsPath = "runtime/settings.json";
    private readonly FileInfo _settingsFileInfo;
    private readonly ILogger<SettingsManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Manages application settings with file-based persistence
    /// </summary>
    internal SettingsManager(string? settingsPath, ILogger<SettingsManager> logger)
    {
        _settingsFileInfo = new(settingsPath ?? SettingsPath);
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

            var json = await File.ReadAllTextAsync(_settingsFileInfo.FullName);
            var settings = JsonSerializer.Deserialize<SettingsModel>(json, _jsonOptions);

            return settings is null
                ? Result.Fail("Failed to deserialize settings")
                : Result.Ok(settings);
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
                return Result.Fail($"Invalid path to settings file [{_settingsFileInfo.FullName}]");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
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
            new("smtp.example.com", 22, "username", "password", true),
            new("apikey", "model", 2048)
        );
}
