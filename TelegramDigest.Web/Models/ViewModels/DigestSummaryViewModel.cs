using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public class DigestSummaryViewModel
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    [Display(Name = "Created At")]
    public required DateTime CreatedAt { get; init; }

    [Required]
    public required string Summary { get; init; }

    [Required]
    public required string Title { get; init; }

    [Required]
    public required int PostsCount { get; init; }

    [Required]
    public required double AverageImportance { get; init; }

    [Required]
    public required DateTime DateFrom { get; init; }

    [Required]
    public required DateTime DateTo { get; init; }
}

public class PostSummaryViewModel
{
    [Required]
    [Display(Name = "Channel Name")]
    public required string ChannelName { get; init; }

    [Required]
    public required string Summary { get; init; }

    [Required]
    [Display(Name = "Original Link")]
    public required string Url { get; init; }

    [Required]
    [Display(Name = "Posted At")]
    public required DateTime PostedAt { get; init; }

    [Required]
    public required int Importance { get; init; }
}
