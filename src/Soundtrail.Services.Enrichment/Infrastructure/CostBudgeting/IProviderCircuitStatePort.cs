using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IProviderCircuitStatePort
{
    Task<ProviderCircuitState> GetAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        ProviderCircuitState state,
        CancellationToken cancellationToken);
}
