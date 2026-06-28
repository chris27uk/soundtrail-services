using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class ApplyMusicCatalogLookupAttemptedToDiscoveryHandler(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand>
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
                    var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, tracking.SearchCriteria, cancellationToken);
                    if (!loaded.Aggregate.LookupStarted(attempted.Priority, attempted.CreatedAt))
                    {
                        continue;
                    }

                    await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
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

            var existingTrackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(fetched.MusicCatalogId, cancellationToken);
            var discoverySearchTerms = resolvedSearchTerms
                .Concat(existingTrackings.Select(static tracking => tracking.SearchCriteria))
                .Distinct();

            foreach (var searchTerm in discoverySearchTerms)
            {
                var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, searchTerm, cancellationToken);
                if (!loaded.Aggregate.LookupCompleted(fetched.Priority, fetched.CreatedAt))
                {
                    continue;
                }

                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }
        }

        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        {
            return;
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, tracking.SearchCriteria, cancellationToken);
            var changed = attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => loaded.Aggregate.LookupDeferred(
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => loaded.Aggregate.LookupFailed(
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                _ => false
            };

            if (!changed)
            {
                continue;
            }

            await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
        }
    }
}
