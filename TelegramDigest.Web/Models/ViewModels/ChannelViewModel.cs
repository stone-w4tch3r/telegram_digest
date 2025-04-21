using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Backend.Core;

namespace TelegramDigest.Web.Models.ViewModels;

public sealed record RssProvider
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
}

public sealed record FeedViewModel
{
    [Required]
    [Display(Name = "Feed Title")]
    public required string Title { get; init; }

    [Required]
    [Display(Name = "Feed URL")]
    [Url]
    public required string Url { get; init; }
}

[Display(Name = "Feed")]
public sealed record AddFeedViewModel
{
    [Required]
    [Display(Name = "Feed URL")]
    [Url]
    public required string FeedUrl { get; init; }
}

public sealed record ChannelViewModel
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

public sealed record AddChannelViewModel
{
    [Required]
    [Display(Name = "Channel Id")]
    [ModelBinder(typeof(ChannelTgIdModelBinder))]
    public required ChannelTgId TgId { get; init; }
}
