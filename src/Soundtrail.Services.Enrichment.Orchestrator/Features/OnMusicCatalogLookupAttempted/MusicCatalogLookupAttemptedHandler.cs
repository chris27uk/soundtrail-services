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
        { }
        else
        {
            if (attempted.Outcome.Status is MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Duplicate)
            { }
            else
            {
                var trackings2 = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
                foreach (var tracking1 in trackings2)
                {
                    var history2 = await SearchOrSeekHistory.LoadAsync(
                        discoveryRepository,
                        tracking1.SearchCriteria,
                        cancellationToken);
                    if (!history2.Start(attempted.Priority, "Lookup started", attempted.CreatedAt))
                    {
                        continue;
                    }

                    await history2.SaveAsync(discoveryRepository, cancellationToken);
                }
            }
        }

        if (attempted.MusicCatalogMetadataFetched is not null)
        {
            var aggregate = await CatalogEntityAggregate.LoadAsync(
                eventRepository,
                attempted.MusicCatalogMetadataFetched.MusicCatalogId,
                cancellationToken);
            aggregate.RecordMusicCatalogMetadataFetched(attempted.MusicCatalogMetadataFetched);
            await aggregate.SaveAsync(eventRepository, attempted.MusicCatalogMetadataFetched.CommandId, cancellationToken);

            var resolvedSearchTerms = MusicSearchTermSet.ForResolvedTrack(
                attempted.MusicCatalogMetadataFetched.MusicCatalogId,
                attempted.MusicCatalogMetadataFetched.Hierarchy?.ArtistId,
                attempted.MusicCatalogMetadataFetched.Hierarchy?.AlbumId);

            foreach (var searchTerm in resolvedSearchTerms)
            {
                await catalogSearchTrackingStore.UpsertAsync(
                    new CatalogSearchTracking(
                        searchTerm,
                        attempted.MusicCatalogMetadataFetched.MusicCatalogId,
                        attempted.MusicCatalogMetadataFetched.CreatedAt),
                    cancellationToken);
            }

            var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogMetadataFetched.MusicCatalogId, cancellationToken);
            var discoverySearchTerms = resolvedSearchTerms
                .Concat(trackings.Select(static tracking => tracking.SearchCriteria))
                .Distinct();

            foreach (var searchTerm in discoverySearchTerms)
            {
                var history = await SearchOrSeekHistory.LoadAsync(
                    discoveryRepository,
                    searchTerm,
                    cancellationToken);
                if (!history.Complete(attempted.MusicCatalogMetadataFetched.Priority, "Discovery completed", attempted.MusicCatalogMetadataFetched.CreatedAt))
                {
                    continue;
                }

                await history.SaveAsync(discoveryRepository, cancellationToken);
            }
        }

        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        { }
        else
        {
            var trackings1 = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
            foreach (var tracking in trackings1)
            {
                var history1 = await SearchOrSeekHistory.LoadAsync(
                    discoveryRepository,
                    tracking.SearchCriteria,
                    cancellationToken);
                var changed = attempted.Outcome.Status switch
                {
                    MusicCatalogLookupOutcomeStatus.Deferred => history1.Defer(
                        attempted.Outcome.RetryAfterSeconds,
                        attempted.Outcome.RetryAt,
                        attempted.Outcome.Reason ?? "Lookup deferred",
                        attempted.CreatedAt),
                    MusicCatalogLookupOutcomeStatus.Failed => history1.Fail(
                        attempted.Outcome.Reason ?? "Lookup failed",
                        attempted.CreatedAt),
                    _ => false
                };

                if (!changed)
                {
                    continue;
                }

                await history1.SaveAsync(discoveryRepository, cancellationToken);
            }
        }

        return new EnrichmentOrchestrationResult(Array.Empty<IMusicTrackEvent>());
    }
}
