using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Application.Database;

internal sealed class ChannelEntity
{
    [MaxLength(32)]
    public required string TgId { get; init; } = null!;

    [MaxLength(100)]
    public required string Title { get; init; } = null!;

    [MaxLength(1000)]
    public required string Description { get; init; } = null!;

    [MaxLength(2048)]
    public required string ImageUrl { get; init; } = null!;
}

internal sealed class DigestEntity
{
    public required Guid Id { get; init; }

    public DigestSummaryEntity? SummaryNav { get; init; }

    public ICollection<PostSummaryEntity>? PostsNav { get; init; }
}

internal sealed class DigestSummaryEntity
{
    public required Guid Id { get; init; }

    [MaxLength(200)]
    public required string Title { get; init; } = null!;

    [MaxLength(8192)]
    public required string PostsSummary { get; init; } = null!;

    public required int PostsCount { get; init; }

    public required double AverageImportance { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime DateFrom { get; init; }

    public required DateTime DateTo { get; init; }

    [MaxLength(2048)]
    public required string ImageUrl { get; init; } = null!;

    public DigestEntity? DigestNav { get; init; }
}

internal sealed class PostSummaryEntity
{
    public required Guid Id { get; init; }

    [MaxLength(32)]
    public required string ChannelTgId { get; init; } = null!;

    [MaxLength(2000)]
    public required string Summary { get; init; } = null!;

    [MaxLength(2048)]
    public required string Url { get; init; } = null!;

    public required DateTime PublishedAt { get; init; }

    [Range(1, 10)]
    public required int Importance { get; init; }

    public ChannelEntity? ChannelNav { get; init; }
}
