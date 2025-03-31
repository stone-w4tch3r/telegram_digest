using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramDigest.Backend.Core;

internal interface IDigestProcessingOrchestrator
{
    Task<Result<DigestGenerationResultModelEnum>> ProcessDigestForLastPeriod(
        DigestId digestId,
        CancellationToken ct
    );

    void QueueDigestForLastPeriod(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestProcessingOrchestrator(
    IDigestService digestService,
    IDigestStepsService digestStepsService,
    ITaskScheduler<DigestId> taskScheduler,
    ISettingsManager settingsManager,
    ILogger<DigestProcessingOrchestrator> logger
) : IDigestProcessingOrchestrator
{
    public async Task<Result<DigestGenerationResultModelEnum>> ProcessDigestForLastPeriod(
        DigestId digestId,
        CancellationToken ct
    )
    {
        digestStepsService.AddStep(
            new SimpleStepModel
            {
                DigestId = digestId,
                Type = DigestStepTypeModelEnum.ProcessingStarted,
            }
        );

        var settings = await settingsManager.LoadSettings(ct);
        if (settings.IsFailed)
        {
            return Result.Fail(settings.Errors);
        }

        //TODO handle 00:00
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var generationResult = await digestService.GenerateDigest(digestId, dateFrom, dateTo, ct);
        if (generationResult.IsFailed)
        {
            logger.LogError(
                "Failed to generate digest: {Errors}",
                string.Join(", ", generationResult.Errors)
            );
            return Result.Fail(generationResult.Errors);
        }

        logger.LogInformation("Digest generation completed successfully");
        return Result.Ok(generationResult.Value);
    }

    public void QueueDigestForLastPeriod(DigestId digestId, CancellationToken ct)
    {
        digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Queued }
        );

        taskScheduler.AddTaskToWaitQueue(
            async (localCt, scope) =>
            {
                // use own scope and services to avoid issues with disposing of captured scope
                var mainService = scope.ServiceProvider.GetRequiredService<IMainService>();
                var mergedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, localCt).Token;
                await mainService.ProcessDigestForLastPeriod(digestId, mergedCt);
            },
            digestId,
            ex =>
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogInformation("Digest {DigestId} processing was canceled", digestId);
                    digestStepsService.AddStep(
                        new SimpleStepModel
                        {
                            DigestId = digestId,
                            Type = DigestStepTypeModelEnum.Queued,
                        }
                    );
                }
                else
                {
                    logger.LogError(
                        ex,
                        "Unhandled exception while trying to process digest {DigestId}",
                        digestId
                    );
                    digestStepsService.AddStep(
                        new ErrorStepModel
                        {
                            DigestId = digestId,
                            Exception = ex,
                            Message = "Unhandled exception while trying to process digest",
                        }
                    );
                }

                return Task.CompletedTask;
            }
        );
    }
}
