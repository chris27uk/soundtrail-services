using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
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
                await SearchOrSeekHistory.ApplyToSearchCriteriaAsync(
                    discoveryRepository,
                    existingTrackings.Select(static tracking => tracking.SearchCriteria),
                    history => history.LookupStarted(attempted.Priority, attempted.CreatedAt),
                    cancellationToken);
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

            await SearchOrSeekHistory.ApplyToSearchCriteriaAsync(
                discoveryRepository,
                discoverySearchTerms,
                history => history.LookupCompleted(fetched.Priority, fetched.CreatedAt),
                cancellationToken);
        }

        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        {
            return;
        }

        await SearchOrSeekHistory.ApplyToTrackingsAsync(
            catalogSearchTrackingStore,
            discoveryRepository,
            attempted.MusicCatalogId,
            history => attempted.Outcome.Status switch
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
            },
            cancellationToken);
    }
}
