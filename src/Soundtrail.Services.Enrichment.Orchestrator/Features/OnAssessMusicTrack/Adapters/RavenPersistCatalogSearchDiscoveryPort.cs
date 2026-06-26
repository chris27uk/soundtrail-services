using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Adapters;

public sealed class RavenPersistCatalogSearchDiscoveryPort(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository) : IPersistCatalogSearchDiscoveryPort
{
    public Task RequestAsync(
        CatalogSearchCriteria criteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken) =>
        PersistAsync(
            criteria,
            discovery => discovery.Request(
                criteria,
                trustLevel,
                riskScore,
                requestedAt,
                correlationId),
            cancellationToken);

    public async Task ApplyToTrackingsAsync(
        MusicCatalogId musicCatalogId,
        Func<CatalogSearchDiscovery, bool> apply,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            await PersistAsync(tracking.Criteria, apply, cancellationToken);
        }
    }

    private async Task PersistAsync(
        CatalogSearchCriteria criteria,
        Func<CatalogSearchDiscovery, bool> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, criteria, cancellationToken);
            if (!apply(discovery))
            {
                return;
            }

            if (await discovery.SaveAsync(discoveryRepository, cancellationToken))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Unable to persist discovery lifecycle for {criteria.Value} after retry.");
    }
}
