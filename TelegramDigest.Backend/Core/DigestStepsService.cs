using FluentResults;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Core;

internal interface IDigestStepsService
{
    Task<Result> AddStep(IDigestStepModel newStep, CancellationToken ct);
    Task<Result<IDigestStepModel[]>> GetAllSteps(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestStepsService(
    IDigestStepsRepository repository,
    ILogger<DigestStepsService> logger
) : IDigestStepsService
{
    public async Task<Result> AddStep(IDigestStepModel newStep, CancellationToken ct)
    {
        return await repository.SaveStepAsync(newStep, ct);
    }

    public async Task<Result<IDigestStepModel[]>> GetAllSteps(
        DigestId digestId,
        CancellationToken ct
    )
    {
        return await repository.LoadStepsHistory(digestId, ct);
    }
}
