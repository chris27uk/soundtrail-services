using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;

public sealed class ApplyMusicCatalogLookupHistoryChangedToSearchDiscoveryHandler(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<MusicCatalogLookupHistoryChangedCommand>
{
    public async Task Handle(
        MusicCatalogLookupHistoryChangedCommand command,
        CancellationToken cancellationToken = default)
    {
        var history = MusicCatalogLookupHistory.Replay(command.Events.Select(static x => x.Event));

        foreach (var (_, @event) in command.Events)
        {
            if (!TryGetMusicCatalogId(@event, out var musicCatalogId))
            {
                continue;
            }

            var fallbackSearchTerms = await ResolveFallbackSearchTermsAsync(musicCatalogId, cancellationToken);
            foreach (var searchTerm in history.ResolveSearchTerms(@event, fallbackSearchTerms))
            {
                var loaded = await SearchDiscoveryHistory.LoadAsync(discoveryRepository, searchTerm, cancellationToken);
                history.ApplyToSearchDiscovery(loaded.Aggregate, @event);
                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<MusicSearchCriteria>> ResolveFallbackSearchTermsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        return trackings.Select(static tracking => tracking.SearchCriteria).Distinct().ToArray();
    }

    private static bool TryGetMusicCatalogId(IDomainEvent @event, out MusicCatalogId musicCatalogId)
    {
        switch (@event)
        {
            case MusicCatalogLookupStarted started:
                musicCatalogId = started.MusicCatalogId;
                return true;
            case MusicCatalogLookupCompleted completed:
                musicCatalogId = completed.MusicCatalogId;
                return true;
            case MusicCatalogLookupDeferred deferred:
                musicCatalogId = deferred.MusicCatalogId;
                return true;
            case MusicCatalogLookupFailed failed:
                musicCatalogId = failed.MusicCatalogId;
                return true;
            default:
                musicCatalogId = null!;
                return false;
        }
    }
}
