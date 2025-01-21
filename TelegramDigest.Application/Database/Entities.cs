namespace TelegramDigest.Application.Database;

public class ChannelEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
}

public class DigestEntity
{
    public Guid Id { get; set; }
    public List<PostSummaryEntity> Posts { get; set; } = new();
    public DigestSummaryEntity Summary { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class PostSummaryEntity
{
    public Guid Id { get; set; }
    public string ChannelId { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime PublishedAt { get; set; }
    public int Importance { get; set; }

    public Guid DigestId { get; set; }
    public DigestEntity Digest { get; set; } = null!;
}

public class DigestSummaryEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string PostsSummary { get; set; } = null!;
    public int PostsCount { get; set; }
    public int AverageImportance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string ImageUrl { get; set; } = null!;

    public DigestEntity Digest { get; set; } = null!;
}
