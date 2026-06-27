using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class SearchCatalogRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    ICatalogSearchDiscoveryRepository catalogSearchDiscoveryRepository,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    public async Task Handle(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken = default)
    {
        var searchHistory = await SearchOrSeekHistory.LoadAsync(
            catalogSearchDiscoveryRepository,
            requested.SearchCriteria,
            cancellationToken);

        var matches = await musicCatalogCandidateSearch.SearchAsync(requested.SearchCriteria, cancellationToken);
        var selectedMatches = new MusicTrackSearchMatchCollection(matches).Query(requested.SearchCriteria);

        if (selectedMatches.Count == 0)
        {
            searchHistory.MetadataRequired(
                requested.TrustLevel,
                requested.RiskScore,
                requested.OccurredAt,
                requested.CorrelationId);

            await searchHistory.SaveAsync(catalogSearchDiscoveryRepository, cancellationToken);
            return;
        }

        foreach (var selectedMatch in selectedMatches)
        {
            var matchedTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(selectedMatch.MusicCatalogId, cancellationToken);
            if (!RequiresStreamingLocationsLookup(matchedTrack, requested.Playback))
            {
                continue;
            }

            searchHistory.StreamingLocationsRequired(
                selectedMatch.MusicCatalogId,
                LookupPriorityBand.Low,
                requested.OccurredAt,
                requested.CorrelationId,
                matchedTrack!.ToSearchTerm(),
                ToHierarchy(matchedTrack));
        }

        await searchHistory.SaveAsync(catalogSearchDiscoveryRepository, cancellationToken);
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult localTrack) =>
        localTrack.ArtistId is null && localTrack.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack.ArtistId, localTrack.AlbumId);

    private static bool RequiresStreamingLocationsLookup(
        LocalMusicTrackSearchResult? localTrack,
        PlaybackProviderFilter playback) =>
        localTrack is not null
        && localTrack.CanCreateSearchTerm()
        && localTrack.RequiresStreamingLocations(playback);
}
