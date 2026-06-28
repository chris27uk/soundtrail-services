using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class SearchCatalogRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> catalogSearchDiscoveryRepository,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<SearchCatalogRequested>
{
    public async Task Handle(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(requested.SearchCriteria, cancellationToken);
        var followUp = await new MusicTrackSearchMatchCollection(matches)
            .DetermineFollowUpAsync(
                requested.SearchCriteria,
                requested.Playback,
                localMusicTrackSearch,
                cancellationToken);

        await SearchOrSeekHistory.ApplyAsync(
            catalogSearchDiscoveryRepository,
            requested.SearchCriteria,
            history =>
            {
                if (followUp.RequiresTrackMetadataLookup)
                {
                    return history.TrackMetadataLookupRequested(
                        requested.SearchCriteria,
                        requested.TrustLevel,
                        requested.RiskScore,
                        requested.OccurredAt,
                        requested.CorrelationId);
                }

                var appended = false;
                foreach (var lookup in followUp.StreamingLocationLookups)
                {
                    history.StreamingLocationsRequired(
                        lookup.MusicCatalogId,
                        LookupPriorityBand.Low,
                        requested.OccurredAt,
                        requested.CorrelationId,
                        lookup.SearchCriteria,
                        lookup.Hierarchy);
                    appended = true;
                }

                return appended;
            },
            cancellationToken);
    }
}
