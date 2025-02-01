using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TelegramDigest.Backend.Core;

/// <summary>
/// Background service that schedules and executes daily digest generation
/// </summary>
[UsedImplicitly]
internal sealed class Scheduler(ILogger<Scheduler> logger, IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private Timer? _timer;
    private TimeUtc _lastScheduledTimeUtc = new(TimeOnly.MinValue);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check settings every minute for schedule changes
            using var scope = scopeFactory.CreateScope();
            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
            var mainService = scope.ServiceProvider.GetRequiredService<MainService>();
            await UpdateSchedule(settingsManager, mainService);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    /// <summary>
    /// Updates the schedule based on settings and creates/updates timer if needed
    /// </summary>
    private async Task UpdateSchedule(SettingsManager settingsManager, MainService mainService)
    {
        try
        {
            var settingsResult = await settingsManager.LoadSettings();
            if (settingsResult.IsFailed)
            {
                logger.LogError(
                    "Failed to load settings for scheduler: {Errors}",
                    string.Join(", ", settingsResult.Errors)
                );
                return;
            }

            var scheduledTimeUtc = settingsResult.Value.DigestTime;

            // If schedule hasn't changed, no need to update
            if (scheduledTimeUtc.Time == _lastScheduledTimeUtc.Time)
            {
                return;
            }

            _lastScheduledTimeUtc = scheduledTimeUtc;

            // Calculate time until next run
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            var delay = CalculateDelay(now, scheduledTimeUtc.Time);

            logger.LogInformation(
                "Scheduling next digest for {ScheduledTime} (in {Delay})",
                scheduledTimeUtc,
                delay
            );

            // Dispose existing timer if any
            if (_timer != null)
            {
                await _timer.DisposeAsync();
            }

            // Create new timer
            _timer = new(
                _ =>
                    Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteDigestGeneration(mainService);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Unhandled exception in timer callback");
                        }
                    }),
                null,
                delay,
                TimeSpan.FromDays(1) // Repeat daily
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update digest schedule");
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
    private async Task ExecuteDigestGeneration(MainService mainService)
    {
        try
        {
            logger.LogInformation("Starting scheduled digest generation");

            var result = await mainService.ProcessDailyDigest();

            if (result.IsFailed)
            {
                logger.LogError(
                    "Scheduled digest generation failed: {Errors}",
                    string.Join(", ", result.Errors)
                );
            }
            else
            {
                logger.LogInformation("Scheduled digest generation completed successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during scheduled digest generation");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stopping digest scheduler");

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
