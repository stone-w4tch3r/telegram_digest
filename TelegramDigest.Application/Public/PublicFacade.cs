using FluentResults;
using TelegramDigest.Application.Services;

namespace TelegramDigest.Application.Public;

/// <summary>
/// Public API facade for the application, used by Web UI and other external consumers
/// </summary>
public sealed class PublicFacade(MainServiceBase mainService, ILogger<PublicFacade> logger)
{
    public async Task<Result<List<ChannelDto>>> GetChannels()
    {
        var result = await mainService.GetChannels();
        return result.Map(channels => channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result> AddChannel(string channelName)
    {
        return await mainService.AddChannel(ChannelId.From(channelName));
    }

    public async Task<Result> RemoveChannel(string channelName)
    {
        return await mainService.RemoveChannel(ChannelId.From(channelName));
    }

    public async Task<Result<List<DigestPreviewDto>>> GetDigestsSummaries()
    {
        var result = await mainService.GetDigestsSummaries();
        return result.Map(summaries => summaries.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<DigestDto>> GetDigest(Guid digestId)
    {
        var result = await mainService.GetDigest(DigestId.From(digestId));
        return result.Map(digest => digest.ToDto());
    }

    public async Task<Result<DigestPreviewDto>> GenerateDigest()
    {
        var result = await mainService.ProcessDailyDigest();
        if (result.IsFailed)
            return result.ToResult<DigestPreviewDto>();

        var summaries = await GetDigestsSummaries();
        return summaries.IsFailed
            ? summaries.ToResult<DigestPreviewDto>()
            : Result.Ok(summaries.Value.First());
    }

    public async Task<Result<SettingsDto>> GetSettings()
    {
        var result = await mainService.GetSettings();
        return result.Map(settings => new SettingsDto(
            EmailRecipient: settings.EmailRecipient,
            DigestTime: settings.DigestTime,
            SmtpSettings: new SmtpSettingsDto(
                Host: settings.SmtpSettings.Host,
                Port: settings.SmtpSettings.Port,
                Username: settings.SmtpSettings.Username,
                Password: settings.SmtpSettings.Password,
                UseSsl: settings.SmtpSettings.UseSsl
            ),
            OpenAiSettings: new OpenAiSettingsDto(
                ApiKey: settings.OpenAiSettings.ApiKey,
                Model: settings.OpenAiSettings.Model,
                MaxTokens: settings.OpenAiSettings.MaxTokens
            )
        ));
    }

    public async Task<Result> UpdateSettings(SettingsDto settingsDto)
    {
        return await mainService.UpdateSettings(settingsDto.ToDomain());
    }
}
