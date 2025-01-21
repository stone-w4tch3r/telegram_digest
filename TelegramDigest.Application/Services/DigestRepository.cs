using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TelegramDigest.Application.Database;

namespace TelegramDigest.Application.Services;

public class DigestRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DigestRepository> _logger;

    public DigestRepository(ApplicationDbContext dbContext, ILogger<DigestRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public async Task<Result<DigestModel>> LoadDigest(DigestId digestId)
    {
        try
        {
            var digest = await _dbContext
                .Digests.Include(d => d.Posts)
                .FirstOrDefaultAsync(d => d.Id == digestId.Value);

            if (digest == null)
                return Result.Fail(new Error($"Digest {digestId} not found"));

            return Result.Ok(MapToModel(digest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> SaveDigest(DigestModel digest)
    {
        try
        {
            var entity = MapToEntity(digest);
            await _dbContext.Digests.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save digest {DigestId}", digest.DigestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    /// <summary>
    /// Checks if a post is already included in any digest
    /// </summary>
    public async Task<Result<bool>> CheckIfPostIsSaved(PostId postId)
    {
        try
        {
            return Result.Ok(await _dbContext.Posts.AnyAsync(p => p.Id == postId.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check post {PostId}", postId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<DigestSummaryModel>>> LoadAllDigestSummaries()
    {
        try
        {
            var summaries = await _dbContext.DigestSummaries.ToListAsync();
            return Result.Ok(summaries.Select(MapToSummaryModel).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load digest summaries");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    // Mapping methods implementation would go here...
    private DigestModel MapToModel(DigestEntity entity) => throw new NotImplementedException();

    private DigestEntity MapToEntity(DigestModel model) => throw new NotImplementedException();

    private DigestSummaryModel MapToSummaryModel(DigestSummaryEntity entity) =>
        throw new NotImplementedException();
}
