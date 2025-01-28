using FluentResults;
using TelegramDigest.Application.Database;

namespace TelegramDigest.Application.Services;

internal sealed class DigestRepository(
    ApplicationDbContext dbContext,
    ILogger<DigestRepository> logger
)
{
    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    internal async Task<Result<DigestModel>> LoadDigest(DigestId digestId)
    {
        try
        {
            var digest = await dbContext
                .Digests.Include(d => d.Posts)
                .FirstOrDefaultAsync(d => d.Id == digestId.Id);

            if (digest == null)
            {
                return Result.Fail(new Error($"Digest {digestId} not found"));
            }

            return Result.Ok(MapToModel(digest));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    internal async Task<Result> SaveDigest(DigestModel digest)
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
    internal async Task<Result<bool>> CheckIfPostIsSaved(Uri postUrl)
    {
        try
        {
            return Result.Ok(
                await dbContext.PostSummaries.AnyAsync(p => p.Url == postUrl.ToString())
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check post {PostId}", postUrl);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    internal async Task<Result<List<DigestSummaryModel>>> LoadAllDigestSummaries()
    {
        try
        {
            var summaries = await dbContext.DigestSummaries.ToListAsync();
            return Result.Ok(summaries.Select(MapToSummaryModel).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest summaries");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    // Mapping methods implementation would go here...
    private DigestModel MapToModel(DigestEntity entity) => throw new NotImplementedException();

    private DigestEntity MapToEntity(DigestModel model) => throw new NotImplementedException();

    private DigestSummaryModel MapToSummaryModel(DigestSummaryEntity entity) =>
        throw new NotImplementedException();
}
