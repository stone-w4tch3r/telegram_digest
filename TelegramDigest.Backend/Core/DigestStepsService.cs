using System.Threading.Channels;
using FluentResults;
using Microsoft.Extensions.Hosting;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IDigestStepsService
{
    void AddStep(IDigestStepModel step);
    Task<Result<IDigestStepModel[]>> GetAllSteps(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestStepsBackgroundService(
    IDigestStepsRepository repository,
    ILogger<DigestStepsBackgroundService> logger
) : BackgroundService, IDigestStepsService
{
    private readonly Channel<IDigestStepModel> _stepsChannel =
        Channel.CreateUnbounded<IDigestStepModel>(
            new() { SingleReader = true, SingleWriter = false }
        );

    public async Task<Result<IDigestStepModel[]>> GetAllSteps(
        DigestId digestId,
        CancellationToken ct
    )
    {
        return await repository.LoadStepsHistory(digestId, ct);
    }

    public void AddStep(IDigestStepModel step)
    {
        try
        {
            if (!_stepsChannel.Writer.TryWrite(step))
            {
                logger.LogWarning(
                    "Failed to add step to channel for digest {DigestId}",
                    step.DigestId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error while trying to add step for digest {DigestId}",
                step.DigestId
            );
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IDigestStepModel? step = null;
            try
            {
                step = await _stepsChannel.Reader.ReadAsync(stoppingToken);
                await repository.SaveStepAsync(step, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Cancellation requested, Stopping new digest steps processing, "
                );
                break;
            }
            catch (Exception ex)
            {
                if (step != null)
                {
                    logger.LogError(
                        ex,
                        "Failed to save step {Type} for digest {DigestId} with message [{Message}]",
                        step.Type,
                        step.DigestId,
                        step.Message
                    );
                }

                logger.LogError(
                    ex,
                    "Unhandled error while processing steps, stopping new DigestSteps processing"
                );
                break;
            }
        }
    }
}
