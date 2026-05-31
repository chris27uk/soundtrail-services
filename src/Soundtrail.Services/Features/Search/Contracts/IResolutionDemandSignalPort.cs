using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search.Contracts;

public interface IResolutionDemandSignalPort
{
    Task EnqueueAsync(
        ResolutionDemandSignal signal,
        CancellationToken cancellationToken);

    ValueTask<ResolutionDemandSignal?> DequeueAsync(
        CancellationToken cancellationToken);
}

public sealed record ResolutionDemandSignal(
    QueryId QueryId,
    NormalizedSearchQuery Query);
