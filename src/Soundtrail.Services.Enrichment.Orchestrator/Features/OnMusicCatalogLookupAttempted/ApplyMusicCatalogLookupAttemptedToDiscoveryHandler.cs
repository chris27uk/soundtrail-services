using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class ApplyMusicCatalogLookupAttemptedToDiscoveryHandler(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository) : IHandler<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand>
{
    public async Task Handle(
        ApplyMusicCatalogLookupAttemptedToDiscoveryCommand command,
        CancellationToken cancellationToken = default)
    {
        var attempted = command.Attempted;

        if (attempted.MusicCatalogMetadataFetched is null)
        {
            if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Duplicate))
            {
                var existingTrackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
                foreach (var tracking in existingTrackings)
                {
                    var history = await SearchOrSeekHistory.LoadAsync(
                        discoveryRepository,
                        tracking.SearchCriteria,
                        cancellationToken);
                    if (!history.LookupStarted(attempted.Priority, attempted.CreatedAt))
                    {
                        continue;
                    }

                    await history.SaveAsync(discoveryRepository, cancellationToken);
                }
            }
        }

        if (attempted.MusicCatalogMetadataFetched is not null)
        {
            var fetched = attempted.MusicCatalogMetadataFetched;
            var resolvedSearchTerms = MusicSearchTermSet.ForResolvedTrack(
                fetched.MusicCatalogId,
                fetched.Hierarchy?.ArtistId,
                fetched.Hierarchy?.AlbumId);

            foreach (var searchTerm in resolvedSearchTerms)
            {
                await catalogSearchTrackingStore.UpsertAsync(
                    new CatalogSearchTracking(
                        searchTerm,
                        fetched.MusicCatalogId,
                        fetched.CreatedAt),
                    cancellationToken);
            }

            var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(fetched.MusicCatalogId, cancellationToken);
            var discoverySearchTerms = resolvedSearchTerms
                .Concat(trackings.Select(static tracking => tracking.SearchCriteria))
                .Distinct();

            foreach (var searchTerm in discoverySearchTerms)
            {
                var history = await SearchOrSeekHistory.LoadAsync(
                    discoveryRepository,
                    searchTerm,
                    cancellationToken);
                if (!history.LookupCompleted(fetched.Priority, fetched.CreatedAt))
                {
                    continue;
                }

                await history.SaveAsync(discoveryRepository, cancellationToken);
            }
        }

        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        {
            return;
        }

        var trackedSearches = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackedSearches)
        {
            var history = await SearchOrSeekHistory.LoadAsync(
                discoveryRepository,
                tracking.SearchCriteria,
                cancellationToken);
            var changed = attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => history.LookupDeferred(
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => history.LookupFailed(
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                _ => false
            };

            if (!changed)
            {
                continue;
            }

            await history.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
