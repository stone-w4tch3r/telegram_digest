using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Web.Models.ViewModels;

public sealed record class ChannelViewModel
{
    [Required]
    [Display(Name = "Channel Id")]
    [ModelBinder(typeof(ChannelTgIdModelBinder))]
    public required ChannelTgId TgId { get; init; }

    [Required]
    [Display(Name = "Channel Name")]
    public required string Title { get; init; }

    [Required]
    [Display(Name = "Channel URL")]
    [Url]
    public string Url => $"https://t.me/{TgId}";
}

public sealed record class AddChannelViewModel
{
    [Required]
    [Display(Name = "Channel Id")]
    [ModelBinder(typeof(ChannelTgIdModelBinder))]
    public required ChannelTgId TgId { get; init; }
}
