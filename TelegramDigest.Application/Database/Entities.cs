namespace TelegramDigest.Application.Database;

internal sealed class ChannelEntity
{
    internal string Id { get; set; } = null!;
    internal string Name { get; set; } = null!;
    internal string Description { get; set; } = null!;
    internal string ImageUrl { get; set; } = null!;
}

internal sealed class DigestEntity
{
    internal Guid Id { get; set; }
    internal List<PostSummaryEntity> Posts { get; set; } = new();
    internal DigestSummaryEntity Summary { get; set; } = null!;
    internal DateTime CreatedAt { get; set; }
}

internal sealed class PostSummaryEntity
{
    internal string Id { get; set; } = null!;
    internal string Title { get; set; } = null!;
    internal string ChannelId { get; set; } = null!;
    internal string Summary { get; set; } = null!;
    internal string Url { get; set; } = null!;
    internal DateTime PublishedAt { get; set; }
    internal int Importance { get; set; }

    internal Guid DigestId { get; set; }
    internal DigestEntity Digest { get; set; } = null!;
}

internal sealed class DigestSummaryEntity
{
    internal Guid Id { get; set; }
    internal string Title { get; set; } = null!;
    internal string PostsSummary { get; set; } = null!;
    internal int PostsCount { get; set; }
    internal int AverageImportance { get; set; }
    internal DateTime CreatedAt { get; set; }
    internal DateTime DateFrom { get; set; }
    internal DateTime DateTo { get; set; }
    internal string ImageUrl { get; set; } = null!;

    internal DigestEntity Digest { get; set; } = null!;
}
