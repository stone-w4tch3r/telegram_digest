using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TelegramDigest.Backend.DeploymentOptions;

public record BackendDeploymentOptions
{
    public const string MAX_CONCURRENT_AI_TASKS_KEY = "MAX_CONCURRENT_AI_TASKS";
    public const string SQL_LITE_CONNECTION_STRING_KEY = "SQL_LITE_CONNECTION_STRING";

    [Range(1, int.MaxValue, ErrorMessage = "Max concurrent AI tasks must be a positive integer")]
    [Required(ErrorMessage = $"{MAX_CONCURRENT_AI_TASKS_KEY} configuration option was not set")]
    [NotNull]
    public int? MaxConcurrentAiTasks { get; set; }

    [Required(ErrorMessage = $"{SQL_LITE_CONNECTION_STRING_KEY} configuration option was not set")]
    [NotNull]
    public string? SqlLiteConnectionString { get; set; }
}
