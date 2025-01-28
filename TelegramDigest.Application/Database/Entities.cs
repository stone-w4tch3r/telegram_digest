using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Application.Database;

internal sealed class ChannelEntity
{
    [MaxLength(32)]
    internal string TgId { get; init; } = null!;

    [MaxLength(100)]
    internal string Title { get; init; } = null!;

    [MaxLength(1000)]
    internal string Description { get; init; } = null!;

    [MaxLength(2048)]
    internal string ImageUrl { get; init; } = null!;
}

internal sealed class DigestEntity
{
    internal Guid Id { get; init; }

    internal DateTime CreatedAt { get; init; }

    // Navigation properties
    internal DigestSummaryEntity Summary { get; init; } = null!;
    internal ICollection<PostSummaryEntity> Posts { get; init; } = new List<PostSummaryEntity>();
}

internal sealed class PostSummaryEntity
{
    internal Guid Id { get; init; }

    [MaxLength(32)]
    internal string ChannelTgId { get; init; } = null!;

    [MaxLength(2000)]
    internal string Summary { get; init; } = null!;

    [MaxLength(2048)]
    internal string Url { get; init; } = null!;

    internal DateTime PublishedAt { get; init; }

    [Range(1, 10)]
    internal int Importance { get; init; }

    // Navigation properties
    internal Guid DigestId { get; init; }
    internal DigestEntity Digest { get; init; } = null!;
    internal ChannelEntity Channel { get; init; } = null!;
}

internal sealed class DigestSummaryEntity
{
    internal Guid Id { get; init; }

    [MaxLength(200)]
    internal string Title { get; init; } = null!;

    [MaxLength(8192)]
    internal string PostsSummary { get; init; } = null!;

    internal int PostsCount { get; init; }

    internal double AverageImportance { get; init; }

    internal DateTime CreatedAt { get; init; }

    internal DateTime DateFrom { get; init; }

    internal DateTime DateTo { get; init; }

    [MaxLength(2048)]
    internal string ImageUrl { get; init; } = null!;

    // Navigation property
    internal DigestEntity Digest { get; init; } = null!;
}
