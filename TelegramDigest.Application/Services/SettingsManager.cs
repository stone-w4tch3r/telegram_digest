using FluentResults;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TelegramDigest.Application.Services;

public class SettingsManager
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Manages application settings with file-based persistence
    /// </summary>
    public SettingsManager(string settingsPath, ILogger<SettingsManager> logger)
    {
        _settingsPath = settingsPath;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<SettingsModel>> LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return Result.Fail(new Error("Settings file not found"));
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<SettingsModel>(json, _jsonOptions);

            return settings is null 
                ? Result.Fail(new Error("Invalid settings format")) 
                : Result.Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsPath);
            return Result.Fail(new Error("Settings loading failed").CausedBy(ex));
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
            return Result.Fail(new Error("Settings saving failed").CausedBy(ex));
        }
    }
}
