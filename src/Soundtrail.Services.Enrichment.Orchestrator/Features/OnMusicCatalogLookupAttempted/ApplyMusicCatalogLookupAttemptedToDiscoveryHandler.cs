using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
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
            await ApplyLookupCompletedAsync(attempted, cancellationToken);
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

        await UpdateKnownTrackDiscoveryAsync(
            attempted.MusicCatalogId,
            aggregate => aggregate.TrackLookupStarted(
                TrackId.From(attempted.MusicCatalogId.Value),
                attempted.Priority,
                "Lookup started",
                attempted.CreatedAt),
            cancellationToken);

        var searchTerms = await ResolveSearchTermsAsync(attempted, cancellationToken);
        foreach (var searchTerm in searchTerms)
        {
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupStarted(attempted.Priority, attempted.CreatedAt),
                cancellationToken);
        }
    }

    private async Task ApplyLookupCompletedAsync(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken)
    {
        var fetched = attempted.MusicCatalogMetadataFetched
                     ?? throw new InvalidOperationException("Completed lookup attempts must include fetched metadata.");

        await UpdateKnownTrackDiscoveryAsync(
            fetched.MusicCatalogId,
            aggregate => aggregate.TrackLookupCompleted(
                TrackId.From(fetched.MusicCatalogId.Value),
                fetched.Priority,
                "Discovery completed",
                fetched.CreatedAt),
            cancellationToken);

        var resolvedSearchTerms = MusicSearchTermSet.ForResolvedTrack(
            fetched.MusicCatalogId,
            fetched.Hierarchy?.ArtistId,
            fetched.Hierarchy?.AlbumId,
            attempted.SearchCriteria);

        var discoverySearchTerms = resolvedSearchTerms.Count > 0
            ? resolvedSearchTerms
            : (await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(fetched.MusicCatalogId, cancellationToken))
            .Select(static tracking => tracking.SearchCriteria)
            .Distinct()
            .ToArray();

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
        await UpdateKnownTrackDiscoveryAsync(
            attempted.MusicCatalogId,
            aggregate => attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => aggregate.TrackLookupDeferred(
                    TrackId.From(attempted.MusicCatalogId.Value),
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => aggregate.TrackLookupFailed(
                    TrackId.From(attempted.MusicCatalogId.Value),
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                _ => false
            },
            cancellationToken);

        var searchTerms = await ResolveSearchTermsAsync(attempted, cancellationToken);
        foreach (var searchTerm in searchTerms)
        {
            await UpdateDiscoveryAsync(
                searchTerm,
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

    private async Task<IReadOnlyList<MusicSearchCriteria>> ResolveSearchTermsAsync(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken)
    {
        if (attempted.SearchCriteria is not null)
        {
            return [attempted.SearchCriteria];
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(attempted.MusicCatalogId, cancellationToken);
        return trackings
            .Select(static tracking => tracking.SearchCriteria)
            .Distinct()
            .ToArray();
    }

    private async Task UpdateKnownTrackDiscoveryAsync(
        MusicCatalogId musicCatalogId,
        Func<KnownItemDiscovery, bool> apply,
        CancellationToken cancellationToken)
    {
        var knownItem = KnownCatalogItem.ForTrack(TrackId.From(musicCatalogId.Value));
        var loaded = await KnownItemDiscovery.LoadAsync(discoveryRepository, knownItem, cancellationToken);
        if (!loaded.Stream.Events.OfType<Soundtrail.Domain.Discovery.Events.KnownTrackRequested>().Any())
        {
            return;
        }

        if (!apply(loaded.Aggregate))
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
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
