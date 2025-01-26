using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramDigest.Application.Database;

/// <summary>
/// Fluent API configurations for database entities
/// </summary>
internal sealed class ChannelConfiguration : IEntityTypeConfiguration<ChannelEntity>
{
    public void Configure(EntityTypeBuilder<ChannelEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.ImageUrl).IsRequired();
    }
}

internal sealed class DigestConfiguration : IEntityTypeConfiguration<DigestEntity>
{
    public void Configure(EntityTypeBuilder<DigestEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder
            .HasOne(d => d.Summary)
            .WithOne(s => s.Digest)
            .HasForeignKey<DigestSummaryEntity>(s => s.Id);

        builder
            .HasMany(d => d.Posts)
            .WithOne(p => p.Digest)
            .HasForeignKey(p => p.DigestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PostSummaryConfiguration : IEntityTypeConfiguration<PostSummaryEntity>
{
    public void Configure(EntityTypeBuilder<PostSummaryEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ChannelId).IsRequired();
        builder.Property(e => e.Summary).IsRequired();
        builder.Property(e => e.Url).IsRequired();
        builder.Property(e => e.PublishedAt).IsRequired();
        builder.Property(e => e.Importance).IsRequired();
    }
}

internal sealed class DigestSummaryConfiguration : IEntityTypeConfiguration<DigestSummaryEntity>
{
    public void Configure(EntityTypeBuilder<DigestSummaryEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired();
        builder.Property(e => e.PostsSummary).IsRequired();
        builder.Property(e => e.PostsCount).IsRequired();
        builder.Property(e => e.AverageImportance).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.DateFrom).IsRequired();
        builder.Property(e => e.DateTo).IsRequired();
        builder.Property(e => e.ImageUrl).IsRequired();
    }
}
