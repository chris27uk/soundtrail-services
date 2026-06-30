using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;

public sealed class MusicCatalogLookupHistoryChangedHandler(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<MusicCatalogLookupHistoryChangedCommand>
{
    public async Task Handle(
        MusicCatalogLookupHistoryChangedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var (_, @event) in command.Events)
        {
            switch (@event)
            {
                case MusicCatalogLookupStarted started:
                    await ApplyStartedAsync(started, cancellationToken);
                    break;
                case MusicCatalogLookupCompleted completed:
                    await ApplyCompletedAsync(completed, cancellationToken);
                    break;
                case MusicCatalogLookupDeferred deferred:
                    await ApplyDeferredAsync(deferred, cancellationToken);
                    break;
                case MusicCatalogLookupFailed failed:
                    await ApplyFailedAsync(failed, cancellationToken);
                    break;
            }
        }
    }

    private async Task ApplyStartedAsync(MusicCatalogLookupStarted started, CancellationToken cancellationToken)
    {
        await UpdateKnownTrackDiscoveryAsync(
            started.MusicCatalogId,
            aggregate => aggregate.TrackLookupStarted(
                TrackId.From(started.MusicCatalogId.Value),
                started.Priority,
                "Lookup started",
                started.StartedAt),
            cancellationToken);

        foreach (var searchTerm in await ResolveSearchTermsAsync(started.MusicCatalogId, null, cancellationToken))
        {
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupStarted(started.Priority, started.StartedAt),
                cancellationToken);
        }
    }

    private async Task ApplyCompletedAsync(MusicCatalogLookupCompleted completed, CancellationToken cancellationToken)
    {
        await UpdateKnownTrackDiscoveryAsync(
            completed.MusicCatalogId,
            aggregate => aggregate.TrackLookupCompleted(
                TrackId.From(completed.MusicCatalogId.Value),
                completed.Priority,
                "Discovery completed",
                completed.CompletedAt),
            cancellationToken);

        var resolvedSearchTerms = completed.Metadata is not null
            ? MusicSearchTermSet.ForResolvedTrack(
                completed.MusicCatalogId,
                completed.Hierarchy?.ArtistId,
                completed.Hierarchy?.AlbumId,
                completed.SearchCriteria)
            : [];

        var discoverySearchTerms = resolvedSearchTerms.Count > 0
            ? resolvedSearchTerms
            : await ResolveSearchTermsAsync(completed.MusicCatalogId, completed.SearchCriteria, cancellationToken);

        foreach (var searchTerm in discoverySearchTerms)
        {
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupCompleted(completed.Priority, completed.CompletedAt),
                cancellationToken);
        }
    }

    private async Task ApplyDeferredAsync(MusicCatalogLookupDeferred deferred, CancellationToken cancellationToken)
    {
        await UpdateKnownTrackDiscoveryAsync(
            deferred.MusicCatalogId,
            aggregate => aggregate.TrackLookupDeferred(
                TrackId.From(deferred.MusicCatalogId.Value),
                deferred.RetryAfterSeconds,
                deferred.RetryAt,
                deferred.Reason,
                deferred.DeferredAt),
            cancellationToken);

        foreach (var searchTerm in await ResolveSearchTermsAsync(deferred.MusicCatalogId, deferred.SearchCriteria, cancellationToken))
        {
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupDeferred(
                    deferred.RetryAfterSeconds,
                    deferred.RetryAt,
                    deferred.Reason,
                    deferred.DeferredAt),
                cancellationToken);
        }
    }

    private async Task ApplyFailedAsync(MusicCatalogLookupFailed failed, CancellationToken cancellationToken)
    {
        await ApplyStartedAsync(
            new MusicCatalogLookupStarted(
                failed.MusicCatalogId,
                failed.Priority,
                failed.FailedAt),
            cancellationToken);

        await UpdateKnownTrackDiscoveryAsync(
            failed.MusicCatalogId,
            aggregate => aggregate.TrackLookupFailed(
                TrackId.From(failed.MusicCatalogId.Value),
                failed.Priority,
                failed.Reason,
                failed.FailedAt),
            cancellationToken);

        foreach (var searchTerm in await ResolveSearchTermsAsync(failed.MusicCatalogId, failed.SearchCriteria, cancellationToken))
        {
            await UpdateDiscoveryAsync(
                searchTerm,
                aggregate => aggregate.LookupFailed(
                    failed.Priority,
                    failed.Reason,
                    failed.FailedAt),
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<MusicSearchCriteria>> ResolveSearchTermsAsync(
        MusicCatalogId musicCatalogId,
        MusicSearchCriteria? explicitCriteria,
        CancellationToken cancellationToken)
    {
        if (explicitCriteria is not null)
        {
            return [explicitCriteria];
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        return trackings.Select(static tracking => tracking.SearchCriteria).Distinct().ToArray();
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
