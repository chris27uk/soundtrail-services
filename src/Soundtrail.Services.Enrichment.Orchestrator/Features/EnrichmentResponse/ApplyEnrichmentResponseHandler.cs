using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IMusicTrackEventRepository eventRepository,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICompleteTrackedDiscoveriesRepository discoveryRepository)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await CatalogEntityAggregate.LoadAsync(
            eventRepository,
            response.MusicCatalogId,
            cancellationToken);
        aggregate.RecordEnrichmentResponse(response);
        var append = await aggregate.SaveAsync(eventRepository, response.CommandId, cancellationToken);

        var resolvedCriteria = CatalogSearchCriteriaSet.ForResolvedTrack(
            response.MusicCatalogId,
            response.Hierarchy?.ArtistId,
            response.Hierarchy?.AlbumId);

        foreach (var criteria in resolvedCriteria)
        {
            await catalogSearchTrackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    criteria,
                    response.MusicCatalogId,
                    response.CreatedAt),
                cancellationToken);
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(response.MusicCatalogId, cancellationToken);
        var discoveryCriteria = resolvedCriteria
            .Concat(trackings.Select(static tracking => tracking.Criteria))
            .DistinctBy(static criteria => criteria.Value);

        foreach (var criteria in discoveryCriteria)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, criteria, cancellationToken);
            if (!discovery.Complete(response.Priority, "Discovery completed", response.CreatedAt))
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }

        return new EnrichmentOrchestrationResult(append.Appended ? append.AppendedEvents : Array.Empty<IMusicTrackEvent>());
    }
}
