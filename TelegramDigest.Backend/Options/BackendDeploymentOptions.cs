using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace TelegramDigest.Backend.Options;

[NullChecks(false)]
internal record BackendDeploymentOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "Max concurrent AI tasks must be a positive integer")]
    [Required(ErrorMessage = "MAX_CONCURRENT_AI_TASKS configuration option was not set")]
    [ConfigurationKeyName("MAX_CONCURRENT_AI_TASKS")]
    public required int MaxConcurrentAiTasks { get; init; }

    [Required(ErrorMessage = "SQL_LITE_CONNECTION_STRING configuration option was not set")]
    [ConfigurationKeyName("SQL_LITE_CONNECTION_STRING")]
    public required string SqlLiteConnectionString { get; init; }
}
