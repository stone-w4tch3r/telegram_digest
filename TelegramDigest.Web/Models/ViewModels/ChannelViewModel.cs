using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public class ChannelViewModel
{
    public required string TgId { get; init; }

    [Required]
    [Display(Name = "Channel Name")]
    public required string Title { get; init; }

    [Required]
    [Display(Name = "Channel URL")]
    [Url]
    public string Url => $"https://t.me/{TgId}";

    public required string Description { get; init; }

    public required string ImageUrl { get; init; }
}

public class AddChannelViewModel
{
    // TODO validation
    [Required]
    [Display(Name = "Channel Id")]
    public required string TgId { get; init; }
}
