using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class CatalogSearchRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    ICatalogSearchDiscoveryRepository catalogSearchDiscoveryRepository,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    public async Task Handle(
        CatalogSearchRequested requested,
        CancellationToken cancellationToken = default)
    {
        var searchHistory = await SearchOrSeekHistory.LoadAsync(
            catalogSearchDiscoveryRepository,
            requested.Criteria,
            cancellationToken);

        await requested.Criteria.MatchAsync<object?>(async search =>
        {
            var matches = await musicCatalogCandidateSearch.SearchAsync(search, cancellationToken);
            var selectedMatches = new MusicTrackSearchMatchCollection(matches).Query(search);

            if (selectedMatches.Count == 0)
            {
                searchHistory.MetadataRequired(
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.OccurredAt,
                    requested.CorrelationId);

                return null;
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
            return null;
        }, async seek =>
        {
            if (seek.TrackId is null)
            {
                throw new NotSupportedException("Seeking by artist id or album id has not been implemented yet.");
            }

            var matchedTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(
                MusicCatalogId.From(seek.TrackId.Value),
                cancellationToken);

            if (RequiresStreamingLocationsLookup(matchedTrack, requested.Playback))
            {
                searchHistory.StreamingLocationsRequired(
                    matchedTrack!.MusicCatalogId,
                    LookupPriorityBand.Low,
                    requested.OccurredAt,
                    requested.CorrelationId,
                    matchedTrack.ToSearchTerm(),
                    ToHierarchy(matchedTrack));

                return null;
            }

            searchHistory.MetadataRequired(
                requested.TrustLevel,
                requested.RiskScore,
                requested.OccurredAt,
                requested.CorrelationId);

            return null;
        });

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
