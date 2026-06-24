using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class MusicCatalogLookupAttemptedHandler(
    IMusicTrackEventRepository eventRepository,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICompleteTrackedDiscoveriesRepository discoveryRepository)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken = default)
    {
        if (attempted.MusicCatalogMetadataFetched is not null)
        {
            await ApplyFetchedMetadataAsync(attempted.MusicCatalogMetadataFetched, cancellationToken);
        }

        await ApplyOutcomeAsync(attempted, cancellationToken);

        return new EnrichmentOrchestrationResult(Array.Empty<IMusicTrackEvent>());
    }

    private async Task ApplyFetchedMetadataAsync(
        MusicCatalogMetadataFetched fetched,
        CancellationToken cancellationToken)
    {
        var aggregate = await CatalogEntityAggregate.LoadAsync(
            eventRepository,
            fetched.MusicCatalogId,
            cancellationToken);
        aggregate.RecordMusicCatalogMetadataFetched(fetched);
        await aggregate.SaveAsync(eventRepository, fetched.CommandId, cancellationToken);

        var resolvedCriteria = CatalogSearchCriteriaSet.ForResolvedTrack(
            fetched.MusicCatalogId,
            fetched.Hierarchy?.ArtistId,
            fetched.Hierarchy?.AlbumId);

        foreach (var criteria in resolvedCriteria)
        {
            await catalogSearchTrackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    criteria,
                    fetched.MusicCatalogId,
                    fetched.CreatedAt),
                cancellationToken);
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(fetched.MusicCatalogId, cancellationToken);
        var discoveryCriteria = resolvedCriteria
            .Concat(trackings.Select(static tracking => tracking.Criteria))
            .DistinctBy(static criteria => criteria.Value);

        foreach (var criteria in discoveryCriteria)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, criteria, cancellationToken);
            if (!discovery.Complete(fetched.Priority, "Discovery completed", fetched.CreatedAt))
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }

    private async Task ApplyOutcomeAsync(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken)
    {
        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        {
            return;
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);
            var changed = attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => discovery.Defer(
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => discovery.Fail(
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                _ => false
            };

            if (!changed)
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
