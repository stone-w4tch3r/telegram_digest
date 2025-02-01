using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public class ChannelViewModel
{
    //TODO validation
    [Required]
    [Display(Name = "Channel Id")]
    public required string TgId { get; init; }

    [Required]
    [Display(Name = "Channel Name")]
    public required string Title { get; init; }

    [Required]
    [Display(Name = "Channel URL")]
    [Url]
    public string Url => $"https://t.me/{TgId}";

    [Required]
    [Display(Name = "Channel Description")]
    public required string Description { get; init; }

    [Required]
    [Display(Name = "Channel Image")]
    public required string ImageUrl { get; init; }
}

public class AddChannelViewModel
{
    // TODO validation
    [Required]
    [Display(Name = "Channel Id")]
    public required string TgId { get; init; }
}
