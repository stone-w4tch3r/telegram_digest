using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Backend.Database;

/// <summary>
/// Represents a Telegram channel
/// </summary>
internal sealed class ChannelEntity
{
    /// <summary>
    /// Channel ID (part of the channel URL)
    /// </summary>
    [MaxLength(32)]
    public required string TgId { get; init; }

    [MaxLength(100)]
    public required string Title { get; init; }

    [MaxLength(1000)]
    public required string Description { get; init; }

    [MaxLength(2048)]
    public required string ImageUrl { get; init; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public required bool IsDeleted { get; set; }
}

/// <summary>
/// Digest that has multiple posts from different channels
/// </summary>
internal sealed class DigestEntity
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Navigation property to DigestSummaryEntity
    /// </summary>
    public DigestSummaryEntity? SummaryNav { get; init; }

    /// <summary>
    /// Navigation property to PostSummaryEntities
    /// </summary>
    public ICollection<PostSummaryEntity>? PostsNav { get; init; }
}

/// <summary>
/// Metadata for a digest
/// </summary>
internal sealed class DigestSummaryEntity
{
    /// <summary>
    /// Id of Summary but also Foreign Key to Digest
    /// </summary>
    public required Guid Id { get; init; }

    [MaxLength(200)]
    public required string Title { get; init; }

    [MaxLength(8192)]
    public required string PostsSummary { get; init; }

    public required int PostsCount { get; init; }

    public required double AverageImportance { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime DateFrom { get; init; }

    public required DateTime DateTo { get; init; }

    /// <summary>
    /// Navigation property to DigestEntity
    /// </summary>
    public DigestEntity? DigestNav { get; init; }
}

/// <summary>
/// AI generated summary of a post and post info
/// </summary>
internal sealed class PostSummaryEntity
{
    public required Guid Id { get; init; }

    [MaxLength(32)]
    public required string ChannelTgId { get; init; }

    [MaxLength(2000)]
    public required string Summary { get; init; }

    [MaxLength(2048)]
    public required string Url { get; init; }

    public required DateTime PublishedAt { get; init; }

    [Range(1, 10)]
    public required int Importance { get; init; }

    /// <summary>
    /// Foreign key to DigestEntity
    /// </summary>
    public required Guid DigestId { get; init; }

    /// <summary>
    /// Navigation property to ChannelEntity
    /// </summary>
    public ChannelEntity? ChannelNav { get; init; }

    /// <summary>
    /// Navigation property to DigestEntity
    /// </summary>
    public DigestEntity? DigestNav { get; init; }
}
