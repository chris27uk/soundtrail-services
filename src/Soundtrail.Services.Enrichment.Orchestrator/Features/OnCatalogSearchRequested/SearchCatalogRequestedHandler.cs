using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class SearchCatalogRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    ICatalogSearchDiscoveryRepository catalogSearchDiscoveryRepository,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<SearchCatalogRequested>
{
    public async Task Handle(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken = default)
    {
        var history = await SearchOrSeekHistory.LoadAsync(
            catalogSearchDiscoveryRepository,
            requested.SearchCriteria,
            cancellationToken);

        var matches = await musicCatalogCandidateSearch.SearchAsync(requested.SearchCriteria, cancellationToken);
        var followUp = await new MusicTrackSearchMatchCollection(matches)
            .DetermineFollowUpAsync(
                requested.SearchCriteria,
                requested.Playback,
                localMusicTrackSearch,
                cancellationToken);

        followUp.AppendTo(
            history,
            requested.TrustLevel,
            requested.RiskScore,
            requested.OccurredAt,
            requested.CorrelationId);

        await history.SaveAsync(catalogSearchDiscoveryRepository, cancellationToken);
    }
}
