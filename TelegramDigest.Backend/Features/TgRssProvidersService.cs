using Microsoft.Extensions.Options;
using TelegramDigest.Backend.Options;

namespace TelegramDigest.Backend.Features;

using System.Text.Json;
using FluentResults;
using Models;

public interface IRssProvidersService
{
    Task<Result<List<TgRssProviderModel>>> GetProviders(CancellationToken ct);
}

internal class TgRssProvidersService(IOptions<TgRssProvidersOptions> options) : IRssProvidersService
{
    public Task<Result<List<TgRssProviderModel>>> GetProviders(CancellationToken ct)
    {
        try
        {
            var providers = JsonSerializer.Deserialize<List<TgRssProviderModel>>(
                options.Value.TgRssProviders
            );
            if (providers == null)
            {
                return Task.FromResult(
                    Result.Fail<List<TgRssProviderModel>>(
                        $"{nameof(TgRssProvidersOptions.TgRssProviders)} is invalid."
                    )
                );
            }
            return Task.FromResult(Result.Ok(providers));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                Result.Fail<List<TgRssProviderModel>>(
                    $"Failed to deserialize TgRssProviders: {ex.Message}"
                )
            );
        }
    }
}
