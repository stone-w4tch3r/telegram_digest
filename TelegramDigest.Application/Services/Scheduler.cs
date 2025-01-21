namespace TelegramDigest.Application.Services;

/// <summary>
/// Background service that schedules and executes daily digest generation
/// </summary>
public class Scheduler : BackgroundService
{
    private readonly MainService _mainService;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<Scheduler> _logger;
    private Timer? _timer;
    private TimeOnly _lastScheduledTime;

    public Scheduler(
        MainService mainService,
        SettingsManager settingsManager,
        ILogger<Scheduler> logger
    )
    {
        _mainService = mainService;
        _settingsManager = settingsManager;
        _logger = logger;
        _lastScheduledTime = TimeOnly.MinValue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial setup of the timer
        await UpdateSchedule();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Check settings every minute for schedule changes
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            await UpdateSchedule();
        }
    }

    /// <summary>
    /// Updates the schedule based on settings and creates/updates timer if needed
    /// </summary>
    private async Task UpdateSchedule()
    {
        try
        {
            var settingsResult = await _settingsManager.LoadSettings();
            if (settingsResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to load settings for scheduler: {Errors}",
                    string.Join(", ", settingsResult.Errors)
                );
                return;
            }

            var scheduledTime = settingsResult.Value.DigestTime;

            // If schedule hasn't changed, no need to update
            if (scheduledTime == _lastScheduledTime)
                return;

            _lastScheduledTime = scheduledTime;

            // Calculate time until next run
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var delay = CalculateDelay(now, scheduledTime);

            _logger.LogInformation(
                "Scheduling next digest for {ScheduledTime} (in {Delay})",
                scheduledTime,
                delay
            );

            // Dispose existing timer if any
            if (_timer != null)
            {
                await _timer.DisposeAsync();
            }

            // Create new timer
            _timer = new Timer(
                async _ => await ExecuteDigestGeneration(),
                null,
                delay,
                TimeSpan.FromDays(1) // Repeat daily
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update digest schedule");
        }
    }

    /// <summary>
    /// Calculates delay until next scheduled time
    /// </summary>
    private static TimeSpan CalculateDelay(TimeOnly now, TimeOnly scheduledTime)
    {
        var delay = scheduledTime - now;
        if (delay < TimeSpan.Zero)
        {
            // If scheduled time has passed today, schedule for tomorrow
            delay = delay.Add(TimeSpan.FromDays(1));
        }
        return delay;
    }

    /// <summary>
    /// Executes digest generation and handles any errors
    /// </summary>
    private async Task ExecuteDigestGeneration()
    {
        try
        {
            _logger.LogInformation("Starting scheduled digest generation");

            var result = await _mainService.ProcessDailyDigest();

            if (result.IsFailed)
            {
                _logger.LogError(
                    "Scheduled digest generation failed: {Errors}",
                    string.Join(", ", result.Errors)
                );
            }
            else
            {
                _logger.LogInformation("Scheduled digest generation completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during scheduled digest generation");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping digest scheduler");

        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
