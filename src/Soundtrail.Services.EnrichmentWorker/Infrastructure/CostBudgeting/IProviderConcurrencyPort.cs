using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IProviderConcurrencyPort
{
    Task<bool> IsAvailableAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task<ConcurrencyLease?> TryAcquireAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        ConcurrencyLease lease,
        CancellationToken cancellationToken);
}
