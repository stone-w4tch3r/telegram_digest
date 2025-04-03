using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public enum DigestStepViewModelEnum
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

public sealed record class DigestProgressViewModel
{
    public required Guid Id { get; init; }

    [Range(0, 100, ErrorMessage = "PercentComplete must be between 0 and 100.")]
    public required int PercentComplete { get; init; }

    public required DigestStepViewModelEnum? CurrentStep { get; init; }

    public required string? ErrorMessage { get; init; }

    public required DateTime? StartedAt { get; init; }

    public required DateTime? CompletedAt { get; init; }

    [Required]
    public required DigestStepViewModel[] Steps { get; init; }
}

public sealed record DigestStepViewModel
{
    public DigestStepViewModel()
    {
        if (Type != DigestStepViewModelEnum.RssReadingStarted && Channels != null)
        {
            throw new ArgumentException("Channels should be null if Type is not RssReadingStarted");
        }
        if (Type != DigestStepViewModelEnum.RssReadingFinished && PostsCount != null)
        {
            throw new ArgumentException(
                "PostsCount should be null if Type is not RssReadingFinished"
            );
        }
        if (Type != DigestStepViewModelEnum.AiProcessing && PercentComplete != null)
        {
            throw new ArgumentException(
                "PercentComplete should be null if Type is not AiProcessing"
            );
        }
    }

    public required DigestStepViewModelEnum Type { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string? Message { get; init; }

    public required string[]? Channels { get; init; }

    public required int? PercentComplete { get; init; }

    public required int? PostsCount { get; init; }
}
