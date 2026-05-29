using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IProviderCircuitStatePort
{
    Task<ProviderCircuitState> GetAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        ProviderCircuitState state,
        CancellationToken cancellationToken);
}
