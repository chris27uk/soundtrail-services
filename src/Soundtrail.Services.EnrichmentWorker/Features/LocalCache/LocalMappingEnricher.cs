using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Providers;

public sealed class LocalMappingEnricher(IMappingStorePort mappingStore) : IEnrichmentProvider
{
    public EnrichmentStage Stage => EnrichmentStage.LocalMapping;

    public ProviderName Provider => ProviderName.Local;

    public async Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        var mapping = await mappingStore.FindAsync(demand, cancellationToken);
        return mapping is null
            ? new EnrichmentJobResult(EnrichmentOutcome.NotFound)
            : new EnrichmentJobResult(EnrichmentOutcome.Resolved, mapping);
    }
}
