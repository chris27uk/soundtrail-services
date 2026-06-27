using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class RavenRecordCatalogSearchStartedPort(
    ICatalogSearchDiscoveryRepository discoveryRepository) : IRecordCatalogSearchStartedPort
{
    public async Task RecordAsync(
        MusicSearchCriteria criteria,
        IReadOnlyList<MusicCatalogId> musicCatalogIds,
        int trustLevel,
        int riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        if (musicCatalogIds.Count == 0)
        {
            return;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var aggregate = await CatalogSearchStarted.LoadAsync(discoveryRepository, criteria, cancellationToken);

            foreach (var musicCatalogId in musicCatalogIds)
            {
                aggregate.Record(
                    musicCatalogId,
                    trustLevel,
                    riskScore,
                    occurredAt,
                    correlationId);
            }

            if (await aggregate.SaveAsync(discoveryRepository, cancellationToken))
            {
                return;
            }
        }

        throw new InvalidOperationException("Unable to persist search-start events after retry.");
    }
}
