using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Backend.DeploymentOptions;

public record BackendDeploymentOptions
{
    public const string MAX_CONCURRENT_AI_TASKS_KEY = "MAX_CONCURRENT_AI_TASKS";

    [Range(1, int.MaxValue)]
    public int MaxConcurrentAiTasks { get; set; } = 3;
}
