namespace TelegramDigest.Application.Database;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    internal DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    internal DbSet<DigestEntity> Digests => Set<DigestEntity>();
    internal DbSet<PostSummaryEntity> Posts => Set<PostSummaryEntity>();
    internal DbSet<DigestSummaryEntity> DigestSummaries => Set<DigestSummaryEntity>();

    /// <summary>
    /// Applies entity configurations using Fluent API
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ChannelConfiguration());
        modelBuilder.ApplyConfiguration(new DigestConfiguration());
        modelBuilder.ApplyConfiguration(new PostSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new DigestSummaryConfiguration());
    }
}
