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
    /// Checks if a post is already included in any digest
    /// </summary>
    public Task<Result<bool>> CheckIfPostIsSaved(Uri postUrl);

    /// <summary>
    /// Loads all digest summaries from the database
    /// </summary>
    public Task<Result<DigestSummaryModel[]>> LoadAllDigestSummaries();
}

internal sealed class DigestRepository(
    ApplicationDbContext dbContext,
    ILogger<DigestRepository> logger
) : IDigestRepository
{
    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public async Task<Result<DigestModel?>> LoadDigest(DigestId digestId)
    {
        try
        {
            // Include both PostsNav and SummaryNav to get complete digest data
            var digest = await dbContext
                .Digests.Include(d => d.PostsNav)!
                .ThenInclude(p => p.ChannelNav)
                .Include(d => d.SummaryNav)
                .FirstOrDefaultAsync(d => d.Id == digestId.Id);

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

    /// <summary>
    /// Checks if a post is already included in any digest
    /// </summary>
    public async Task<Result<bool>> CheckIfPostIsSaved(Uri postUrl)
    {
        try
        {
            return Result.Ok(
                await dbContext.PostSummaries.AnyAsync(p => p.Url == postUrl.ToString())
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check post {PostUrl}", postUrl);
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

    private static DigestModel MapToModel(DigestEntity entity)
    {
        if (entity.SummaryNav == null || entity.PostsNav == null)
            throw new ArgumentException("Digest entity is incomplete", nameof(entity));

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
            Id = model.DigestId.Id,
            SummaryNav = new()
            {
                Id = model.DigestId.Id,
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
                    Importance = p.Importance.Value,
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
