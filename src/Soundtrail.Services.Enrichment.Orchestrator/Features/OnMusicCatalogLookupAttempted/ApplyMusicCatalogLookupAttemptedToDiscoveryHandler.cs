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
            await ApplyLookupStartedAsync(attempted, cancellationToken);
        }

        if (attempted.MusicCatalogMetadataFetched is not null)
        {
            await ApplyLookupCompletedAsync(attempted.MusicCatalogMetadataFetched, cancellationToken);
        }

        if (attempted.Outcome.Status is not (MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Failed))
        {
            return;
        }

        await ApplyTerminalOutcomeAsync(attempted, cancellationToken);
    }

    private async Task ApplyLookupStartedAsync(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken)
    {
        if (attempted.Outcome.Status is MusicCatalogLookupOutcomeStatus.Deferred or MusicCatalogLookupOutcomeStatus.Duplicate)
        {
            return;
        }

        var existingTrackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        foreach (var tracking in existingTrackings)
        {
            await UpdateDiscoveryAsync(
                tracking.SearchCriteria,
                aggregate => aggregate.LookupStarted(attempted.Priority, attempted.CreatedAt),
                cancellationToken);
        }
    }

    private async Task ApplyLookupCompletedAsync(
        MusicCatalogMetadataFetched fetched,
        CancellationToken cancellationToken)
    {
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
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupCompleted(fetched.Priority, fetched.CreatedAt),
                cancellationToken);
        }
    }

    private async Task ApplyTerminalOutcomeAsync(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            await UpdateDiscoveryAsync(
                tracking.SearchCriteria,
                aggregate => attempted.Outcome.Status switch
                {
                    MusicCatalogLookupOutcomeStatus.Deferred => aggregate.LookupDeferred(
                        attempted.Outcome.RetryAfterSeconds,
                        attempted.Outcome.RetryAt,
                        attempted.Outcome.Reason ?? "Lookup deferred",
                        attempted.CreatedAt),
                    MusicCatalogLookupOutcomeStatus.Failed => aggregate.LookupFailed(
                        attempted.Priority,
                        attempted.Outcome.Reason ?? "Lookup failed",
                        attempted.CreatedAt),
                    _ => false
                },
                cancellationToken);
        }
    }

    private async Task UpdateDiscoveryAsync(
        MusicSearchCriteria searchCriteria,
        Func<SearchDiscoveryHistory, bool> apply,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchDiscoveryHistory.LoadAsync(discoveryRepository, searchCriteria, cancellationToken);
        if (!apply(loaded.Aggregate))
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
