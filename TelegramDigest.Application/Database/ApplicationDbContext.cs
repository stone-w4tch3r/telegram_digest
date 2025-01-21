using Microsoft.EntityFrameworkCore;

namespace TelegramDigest.Application.Database;

public class ApplicationDbContext : DbContext
{
    public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    public DbSet<DigestEntity> Digests => Set<DigestEntity>();
    public DbSet<PostSummaryEntity> Posts => Set<PostSummaryEntity>();
    public DbSet<DigestSummaryEntity> DigestSummaries => Set<DigestSummaryEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

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
