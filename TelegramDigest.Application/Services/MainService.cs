using FluentResults;

namespace TelegramDigest.Application.Services;

/// <summary>
/// Coordinates application services and implements core business logic
/// </summary>
public class MainService
{
    private readonly DigestsService _digestsService;
    private readonly ChannelsService _channelsService;
    private readonly EmailSender _emailSender;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<MainService> _logger;

    public MainService(
        DigestsService digestsService,
        ChannelsService channelsService,
        EmailSender emailSender,
        SettingsManager settingsManager,
        ILogger<MainService> logger
    )
    {
        _digestsService = digestsService;
        _channelsService = channelsService;
        _emailSender = emailSender;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    /// <summary>
    /// Generates and sends daily digest according to configured schedule
    /// </summary>
    public async Task<Result> ProcessDailyDigest()
    {
        _logger.LogInformation("Starting daily digest processing");

        var settings = await _settingsManager.LoadSettings();
        if (settings.IsFailed)
            return settings.ToResult();

        var dateFrom = DateTime.UtcNow.Date.AddDays(-1);
        var dateTo = DateTime.UtcNow.Date;

        var digestResult = await _digestsService.GenerateDigest(dateFrom, dateTo);
        if (digestResult.IsFailed)
            return digestResult.ToResult();

        var digest = await _digestsService.GetDigest(digestResult.Value);
        if (digest.IsFailed)
            return digest.ToResult();

        return await _emailSender.SendDigest(
            digest.Value.DigestSummary,
            settings.Value.EmailRecipient
        );
    }

    public async Task<Result<List<ChannelModel>>> GetChannels() =>
        await _channelsService.GetChannels();

    public async Task<Result> AddChannel(ChannelId channelId) =>
        await _channelsService.AddChannel(channelId);

    public async Task<Result> RemoveChannel(ChannelId channelId) =>
        await _channelsService.RemoveChannel(channelId);

    public async Task<Result<List<DigestSummaryModel>>> GetDigestsSummaries() =>
        await _digestsService.GetDigestsSummaries();

    public async Task<Result<DigestModel>> GetDigest(DigestId digestId) =>
        await _digestsService.GetDigest(digestId);

    public async Task<Result<SettingsModel>> GetSettings() => await _settingsManager.LoadSettings();

    public async Task<Result> UpdateSettings(SettingsModel settings) =>
        await _settingsManager.SaveSettings(settings);
}
