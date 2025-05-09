using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TelegramDigest.Backend.DeploymentOptions;

internal record BackendDeploymentOptions
{
    public const string MAX_CONCURRENT_AI_TASKS_KEY = "MAX_CONCURRENT_AI_TASKS";
    public const string SQL_LITE_CONNECTION_STRING_KEY = "SQL_LITE_CONNECTION_STRING";
    public const string TELEGRAM_RSS_BASE_URL_KEY = "TELEGRAM_RSS_BASE_URL";
    public const string SETTINGS_FILE_PATH_KEY = "SETTINGS_FILE_PATH";

    [Range(1, int.MaxValue, ErrorMessage = "Max concurrent AI tasks must be a positive integer")]
    [Required(ErrorMessage = $"{MAX_CONCURRENT_AI_TASKS_KEY} configuration option was not set")]
    [NotNull]
    public int? MaxConcurrentAiTasks { get; set; }

    [Required(ErrorMessage = $"{SQL_LITE_CONNECTION_STRING_KEY} configuration option was not set")]
    [NotNull]
    public string? SqlLiteConnectionString { get; set; }

    /// <example>"https://rsshub.app/telegram/channel"</example>
    [Required(ErrorMessage = $"{TELEGRAM_RSS_BASE_URL_KEY} configuration option was not set")]
    // [Url(ErrorMessage = $"{TELEGRAM_RSS_BASE_URL_KEY} must be a valid URL")] // TODO use valid http/file/? uri
    [NotNull]
    public Uri? TelegramRssBaseUrl { get; set; }

    [Required(ErrorMessage = $"{SETTINGS_FILE_PATH_KEY} configuration option was not set")]
    [NotNull]
    // TODO use valid file path
    public Uri? SettingsFilePath { get; set; }
}
