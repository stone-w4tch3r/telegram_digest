using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Backend.Db;

internal enum DigestStepTypeEntityEnum
{
    Queued,
    ProcessingStarted,
    RssReadingStarted,
    RssReadingFinished,
    AiProcessing,
    Success,
    Cancelled,
    Error,
    NoPostsFound,
}

internal abstract class DigestStepEntity
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    public required Guid DigestId { get; init; }

    [Required]
    public required DigestStepTypeEntityEnum Type { get; init; }

    [MaxLength(2048)]
    public required string? Message { get; init; }

    public required DateTime Timestamp { get; init; }
}

internal sealed class SimpleStepEntity : DigestStepEntity { }

internal sealed class AiProcessingStepEntity : DigestStepEntity
{
    [Required]
    [Range(0, 100)]
    public required int Percentage { get; init; }
}

internal sealed class RssReadingStartedStepEntity : DigestStepEntity
{
    [Required]
    public required string? FeedsJson { get; init; }
}

internal sealed class RssReadingFinishedStepEntity : DigestStepEntity
{
    [Required]
    public required int PostsFound { get; init; }
}

internal sealed class ErrorStepEntity : DigestStepEntity
{
    public required string? ExceptionJsonSerialized { get; init; }
    public required string? ErrorsJsonSerialized { get; init; }
}
