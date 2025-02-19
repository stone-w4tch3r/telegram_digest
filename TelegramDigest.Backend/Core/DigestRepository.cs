using FluentResults;
using TelegramDigest.Backend.Database;

namespace TelegramDigest.Backend.Core;

internal interface IDigestRepository
{
    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel?>> LoadDigest(DigestId digestId);

    /// <summary>
    /// Saves a digest to the database
    /// </summary>
    public Task<Result> SaveDigest(DigestModel digest);

    /// <summary>
    /// Loads all digest summaries from the database (without posts)
    /// </summary>
    public Task<Result<DigestSummaryModel[]>> LoadAllDigestSummaries();

    /// <summary>
    /// Loads all digests including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel[]>> LoadAllDigests();

    /// <summary>
    /// Deletes a digest and all associated posts.
    /// </summary>
    public Task<Result> DeleteDigest(DigestId digestId);
}

internal sealed class DigestRepository(
    ApplicationDbContext dbContext,
    ILogger<DigestRepository> logger
) : IDigestRepository
{
    public async Task<Result<DigestModel?>> LoadDigest(DigestId digestId)
    {
        try
        {
            // Include both PostsNav and SummaryNav to get complete digest data
            var digest = await dbContext
                .Digests.Include(d => d.PostsNav)!
                .ThenInclude(p => p.ChannelNav)
                .Include(d => d.SummaryNav)
                .FirstOrDefaultAsync(d => d.Id == digestId.Guid);

            if (digest is null)
            {
                return Result.Ok<DigestModel?>(null);
            }
            if (digest.SummaryNav == null || digest.PostsNav == null)
            {
                return Result.Fail(new Error($"Failed to load digest {digestId} from database"));
            }

            return Result.Ok<DigestModel?>(MapToModel(digest));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> SaveDigest(DigestModel digest)
    {
        try
        {
            var entity = MapToEntity(digest);
            await dbContext.Digests.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save digest {DigestId}", digest.DigestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestSummaryModel[]>> LoadAllDigestSummaries()
    {
        try
        {
            var summaries = await dbContext
                .DigestSummaries.OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Result.Ok(summaries.Select(MapToSummaryModel).ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest summaries");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestModel[]>> LoadAllDigests()
    {
        try
        {
            var digests = await dbContext
                .Digests.Include(d => d.PostsNav)!
                .ThenInclude(p => p.ChannelNav)
                .Include(d => d.SummaryNav)
                .ToListAsync();

            if (digests.Any(d => d.SummaryNav == null || d.PostsNav == null))
            {
                return Result.Fail(new Error("Failed to load digests from database"));
            }

            return Result.Ok(digests.Select(MapToModel).ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digests");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteDigest(DigestId digestId)
    {
        try
        {
            var digest = await dbContext
                .Digests.Include(d => d.PostsNav) // Load the related posts
                .Include(d => d.SummaryNav) // Load the related summary
                .FirstOrDefaultAsync(d => d.Id == digestId.Guid);

            if (digest == null)
            {
                return Result.Fail(new Error($"Failed to load digest {digestId}"));
            }
            if (digest.SummaryNav == null)
            {
                return Result.Fail(new Error($"Failed to load summary for digest {digestId}"));
            }
            if (digest.PostsNav == null)
            {
                return Result.Fail(new Error($"Failed to load posts for digest {digestId}"));
            }

            // Remove the digest
            // DigestSummary and related posts should be deleted cascade
            dbContext.Digests.Remove(digest);

            // Ensure related posts and summary are deleted:
            dbContext.DigestSummaries.Remove(digest.SummaryNav);
            dbContext.PostSummaries.RemoveRange(digest.PostsNav);

            await dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    private static DigestModel MapToModel(DigestEntity entity)
    {
        if (entity.SummaryNav == null || entity.PostsNav == null)
        {
            throw new ArgumentException("Digest entity is incomplete", nameof(entity));
        }

        return new(
            DigestId: new(entity.Id),
            PostsSummaries: entity
                .PostsNav.Select(p => new PostSummaryModel(
                    ChannelTgId: new(p.ChannelTgId),
                    Summary: p.Summary,
                    Url: new(p.Url),
                    PublishedAt: p.PublishedAt,
                    Importance: new(p.Importance)
                ))
                .ToList(),
            DigestSummary: MapToSummaryModel(entity.SummaryNav)
        );
    }

    private static DigestEntity MapToEntity(DigestModel model)
    {
        var digestEntity = new DigestEntity
        {
            Id = model.DigestId.Guid,
            SummaryNav = new()
            {
                Id = model.DigestId.Guid,
                Title = model.DigestSummary.Title,
                PostsSummary = model.DigestSummary.PostsSummary,
                PostsCount = model.DigestSummary.PostsCount,
                AverageImportance = model.DigestSummary.AverageImportance,
                CreatedAt = model.DigestSummary.CreatedAt,
                DateFrom = model.DigestSummary.DateFrom,
                DateTo = model.DigestSummary.DateTo,
                DigestNav = null, // Will be set by EF Core
            },
            PostsNav = model
                .PostsSummaries.Select(p => new PostSummaryEntity
                {
                    Id = Guid.NewGuid(),
                    ChannelTgId = p.ChannelTgId.ChannelName,
                    Summary = p.Summary,
                    Url = p.Url.ToString(),
                    PublishedAt = p.PublishedAt,
                    Importance = p.Importance.Number,
                    DigestId = model.DigestId.Guid,
                    DigestNav = null, // Will be set by EF Core
                    ChannelNav = null, // Will be set by EF Core
                })
                .ToList(),
        };

        return digestEntity;
    }

    private static DigestSummaryModel MapToSummaryModel(DigestSummaryEntity entity) =>
        new(
            DigestId: new(entity.Id),
            Title: entity.Title,
            PostsSummary: entity.PostsSummary,
            PostsCount: entity.PostsCount,
            AverageImportance: entity.AverageImportance,
            CreatedAt: entity.CreatedAt,
            DateFrom: entity.DateFrom,
            DateTo: entity.DateTo
        );
}
