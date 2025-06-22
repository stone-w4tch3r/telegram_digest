using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramDigest.Backend.Db;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    internal DbSet<FeedEntity> Feeds => Set<FeedEntity>();
    internal DbSet<DigestEntity> Digests => Set<DigestEntity>();
    internal DbSet<PostSummaryEntity> PostSummaries => Set<PostSummaryEntity>();
    internal DbSet<DigestSummaryEntity> DigestSummaries => Set<DigestSummaryEntity>();
    internal DbSet<DigestStepEntity> DigestSteps => Set<DigestStepEntity>();

    /// <summary>
    /// Applies entity configurations
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FeedConfiguration());
        modelBuilder.ApplyConfiguration(new DigestConfiguration());
        modelBuilder.ApplyConfiguration(new PostSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new DigestSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new DigestStepsConfiguration());
    }

    private sealed class FeedConfiguration : IEntityTypeConfiguration<FeedEntity>
    {
        public void Configure(EntityTypeBuilder<FeedEntity> builder)
        {
            builder.ToTable("Feeds");
            builder.HasKey(e => e.RssUrl);
            builder.Property(e => e.RssUrl).IsRequired();
            builder.Property(e => e.Title).IsRequired();
            builder.Property(e => e.Description).IsRequired();
            builder.Property(e => e.ImageUrl).IsRequired();
            builder.Property(e => e.IsDeleted).IsRequired();
            builder.HasIndex(e => e.IsDeleted);
        }
    }

    private sealed class DigestConfiguration : IEntityTypeConfiguration<DigestEntity>
    {
        public void Configure(EntityTypeBuilder<DigestEntity> builder)
        {
            builder.ToTable("Digests");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.UsedPrompts).HasColumnType("json");

            // 1:1 relationship with DigestSummary
            builder
                .HasOne(d => d.SummaryNav)
                .WithOne(s => s.DigestNav)
                .HasForeignKey<DigestSummaryEntity>(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship with PostSummaries
            builder
                .HasMany(d => d.PostsNav)
                .WithOne(p => p.DigestNav)
                .HasForeignKey(p => p.DigestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    private sealed class PostSummaryConfiguration : IEntityTypeConfiguration<PostSummaryEntity>
    {
        public void Configure(EntityTypeBuilder<PostSummaryEntity> builder)
        {
            builder.ToTable("PostSummaries");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.FeedUrl).IsRequired();
            builder.Property(e => e.Summary).IsRequired();
            builder.Property(e => e.Url).IsRequired();
            builder.Property(e => e.PublishedAt).IsRequired();
            builder.Property(e => e.Importance).IsRequired();

            // Relationship with Feed
            builder
                .HasOne(p => p.FeedNav)
                .WithMany()
                .HasForeignKey(p => p.FeedUrl)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    private sealed class DigestSummaryConfiguration : IEntityTypeConfiguration<DigestSummaryEntity>
    {
        public void Configure(EntityTypeBuilder<DigestSummaryEntity> builder)
        {
            builder.ToTable("DigestSummaries");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Title).IsRequired();
            builder.Property(e => e.PostsSummary).IsRequired();
            builder.Property(e => e.PostsCount).IsRequired();
            builder.Property(e => e.AverageImportance).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.DateFrom).IsRequired();
            builder.Property(e => e.DateTo).IsRequired();
        }
    }

    private sealed class DigestStepsConfiguration : IEntityTypeConfiguration<DigestStepEntity>
    {
        public void Configure(EntityTypeBuilder<DigestStepEntity> builder)
        {
            builder.ToTable("DigestStatuses");

            // Configure TPH
            builder
                .HasDiscriminator<string>("EntityType")
                .HasValue<SimpleStepEntity>("Simple")
                .HasValue<AiProcessingStepEntity>("AiProcessing")
                .HasValue<RssReadingStartedStepEntity>("RssReadingStarted")
                .HasValue<RssReadingFinishedStepEntity>("RssReadingFinished")
                .HasValue<ErrorStepEntity>("Error");

            // Index for faster lookups by DigestId
            builder.HasIndex(e => e.DigestId);

            // Index for faster lookups by Type
            builder.HasIndex(e => e.Type);
        }
    }
}
