using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public enum DigestStatus
{
    InProgress,
    Completed,
    Failed,
}

public sealed record class DigestProgressViewModel
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    [Range(0, 100, ErrorMessage = "PercentComplete must be between 0 and 100.")]
    public required int PercentComplete { get; init; }

    [Required]
    public required DigestStatus Status { get; init; }

    [Required]
    public required string? Message { get; init; }

    [Required]
    public required DateTime StartedAt { get; init; }

    [Required]
    public required DateTime? CompletedAt { get; init; }
}
