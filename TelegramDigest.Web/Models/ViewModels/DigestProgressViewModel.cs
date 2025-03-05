using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public enum DigestStatus
{
    InProgress,
    Completed,
    Failed,
}

public class DigestProgressViewModel
{
    [Required]
    public required Guid Id { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "PercentComplete must be between 0 and 100.")]
    public required int PercentComplete { get; set; }

    [Required]
    public required DigestStatus Status { get; set; }

    [Required]
    public required string? Message { get; set; }

    [Required]
    public required DateTime StartedAt { get; set; }

    [Required]
    public required DateTime? CompletedAt { get; set; }
}
