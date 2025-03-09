using FluentResults;

namespace TelegramDigest.Backend.Core;

internal interface IDigestStepsService
{
    public Task<Result> AddStep(IDigestStepModel newStep);
    public Task<Result<IDigestStepModel[]>> GetAllSteps(DigestId digestId);
}

internal sealed class DigestStepsService(
    IDigestStepsRepository repository,
    ILogger<DigestStepsService> logger
) : IDigestStepsService
{
    public async Task<Result> AddStep(IDigestStepModel newStep)
    {
        return await repository.SaveStepAsync(newStep);
    }

    public async Task<Result<IDigestStepModel[]>> GetAllSteps(DigestId digestId)
    {
        return await repository.LoadStepsHistory(digestId);
    }
}
