using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public class DigestSummaryViewModel
{
    public required Guid Id { get; set; }

    [Display(Name = "Created At")]
    public required DateTime CreatedAt { get; set; }

    public required string Summary { get; set; }

    public required List<PostSummaryViewModel> Posts { get; set; }

    public required string Title { get; set; }

    public required int PostsCount { get; set; }

    public required double AverageImportance { get; set; }

    public required DateTime DateFrom { get; set; }

    public required DateTime DateTo { get; set; }
}

public class PostSummaryViewModel
{
    [Display(Name = "Channel Name")]
    public required string ChannelName { get; set; }

    public required string Summary { get; set; }

    [Display(Name = "Original Link")]
    public required string Url { get; set; }

    [Display(Name = "Posted At")]
    public required DateTime PostedAt { get; set; }

    public required int Importance { get; set; }
}
