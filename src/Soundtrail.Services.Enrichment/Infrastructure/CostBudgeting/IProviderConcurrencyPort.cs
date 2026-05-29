using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

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
