using FluentResults;
using TelegramDigest.Application.Services;

namespace TelegramDigest.Application.Public;

/// <summary>
/// Public API facade for the application, used by Web UI and other external consumers
/// </summary>
public class PublicFacade
{
    private readonly MainService _mainService;
    private readonly ILogger<PublicFacade> _logger;

    public PublicFacade(MainService mainService, ILogger<PublicFacade> logger)
    {
        _mainService = mainService;
        _logger = logger;
    }

    public async Task<Result<List<ChannelDto>>> GetChannels()
    {
        var result = await _mainService.GetChannels();
        return result.Map(channels => channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result> AddChannel(string channelName)
    {
        return await _mainService.AddChannel(ChannelId.From(channelName));
    }

    public async Task<Result> RemoveChannel(string channelName)
    {
        return await _mainService.RemoveChannel(ChannelId.From(channelName));
    }

    public async Task<Result<List<DigestPreviewDto>>> GetDigestsSummaries()
    {
        var result = await _mainService.GetDigestsSummaries();
        return result.Map(summaries => summaries.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<DigestDto>> GetDigest(Guid digestId)
    {
        var result = await _mainService.GetDigest(DigestId.From(digestId));
        return result.Map(digest => digest.ToDto());
    }

    public async Task<Result<DigestPreviewDto>> GenerateDigest()
    {
        var result = await _mainService.ProcessDailyDigest();
        if (result.IsFailed)
            return result.ToResult<DigestPreviewDto>();

        var summaries = await GetDigestsSummaries();
        if (summaries.IsFailed)
            return summaries.ToResult<DigestPreviewDto>();

        return Result.Ok(summaries.Value.First());
    }

    public async Task<Result<SettingsDto>> GetSettings()
    {
        var result = await _mainService.GetSettings();
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
        return await _mainService.UpdateSettings(settingsDto.ToDomain());
    }
}
