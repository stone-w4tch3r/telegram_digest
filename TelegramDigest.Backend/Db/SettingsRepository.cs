using FluentResults;
using TelegramDigest.Backend.Models;

namespace TelegramDigest.Backend.Db;

internal interface ISettingsRepository
{
    Task<Result<SettingsModel?>> LoadSettings(CancellationToken ct);
    Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct);
}

internal sealed class SettingsRepository(
    ApplicationDbContext dbContext,
    ILogger<SettingsRepository> logger
) : ISettingsRepository
{
    public async Task<Result<SettingsModel?>> LoadSettings(CancellationToken ct)
    {
        try
        {
            var entity = await dbContext.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
            return entity == null
                ? Result.Ok<SettingsModel?>(null)
                : Result.Ok<SettingsModel?>(MapToModel(entity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load settings from database");
            return Result.Fail(new Error("Failed to load settings from database").CausedBy(ex));
        }
    }

    public async Task<Result> SaveSettings(SettingsModel settings, CancellationToken ct)
    {
        try
        {
            var entity = MapToEntity(settings);
            var existing = await dbContext.Settings.FirstOrDefaultAsync(ct); // TODO add user
            if (existing != null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await dbContext.Settings.AddAsync(entity, ct);
            }
            await dbContext.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save settings to database");
            return Result.Fail(new Error("Failed to save settings to database").CausedBy(ex));
        }
    }

    private static SettingsModel MapToModel(SettingsEntity entity)
    {
        return new(
            entity.EmailRecipient,
            new(TimeOnly.Parse(entity.DigestTimeUtc)),
            new(
                new(entity.SmtpSettingsHost),
                entity.SmtpSettingsPort,
                entity.SmtpSettingsUsername,
                entity.SmtpSettingsPassword,
                entity.SmtpSettingsUseSsl
            ),
            new(
                entity.OpenAiSettingsApiKey,
                entity.OpenAiSettingsModel,
                entity.OpenAiSettingsMaxTokens,
                new(entity.OpenAiSettingsEndpoint)
            ),
            new(
                new(entity.PromptSettingsPostSummaryUserPrompt),
                new(entity.PromptSettingsPostImportanceUserPrompt),
                new(entity.PromptSettingsDigestSummaryUserPrompt)
            )
        );
    }

    private static SettingsEntity MapToEntity(SettingsModel model)
    {
        return new()
        {
            EmailRecipient = model.EmailRecipient,
            DigestTimeUtc = model.DigestTime.ToString(),
            SmtpSettingsHost = model.SmtpSettings.Host.ToString(),
            SmtpSettingsPort = model.SmtpSettings.Port,
            SmtpSettingsUsername = model.SmtpSettings.Username,
            SmtpSettingsPassword = model.SmtpSettings.Password,
            SmtpSettingsUseSsl = model.SmtpSettings.UseSsl,
            OpenAiSettingsApiKey = model.OpenAiSettings.ApiKey,
            OpenAiSettingsModel = model.OpenAiSettings.Model,
            OpenAiSettingsMaxTokens = model.OpenAiSettings.MaxTokens,
            OpenAiSettingsEndpoint = model.OpenAiSettings.Endpoint.ToString(),
            PromptSettingsPostSummaryUserPrompt =
                model.PromptSettings.PostSummaryUserPrompt.ToString(),
            PromptSettingsPostImportanceUserPrompt =
                model.PromptSettings.PostImportanceUserPrompt.ToString(),
            PromptSettingsDigestSummaryUserPrompt =
                model.PromptSettings.DigestSummaryUserPrompt.ToString(),
        };
    }
}
