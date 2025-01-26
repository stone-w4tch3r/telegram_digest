using System.Text.Json;
using FluentResults;

namespace TelegramDigest.Application.Services;

internal sealed class SettingsManager
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

    internal async Task<Result<SettingsModel>> LoadSettings()
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

    internal async Task<Result> SaveSettings(SettingsModel settings)
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
