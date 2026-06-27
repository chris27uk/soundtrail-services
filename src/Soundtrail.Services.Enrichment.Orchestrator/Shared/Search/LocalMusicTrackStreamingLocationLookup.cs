using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public static class LocalMusicTrackStreamingLocationLookup
{
    public static StreamingLocationLookupCandidate? CreateIfRequired(
        LocalMusicTrackSearchResult? localTrack,
        PlaybackProviderFilter playback)
    {
        if (localTrack is null
            || !localTrack.CanCreateSearchTerm()
            || !localTrack.RequiresStreamingLocations(playback))
        {
            return null;
        }

        return new StreamingLocationLookupCandidate(
            localTrack.MusicCatalogId,
            localTrack.ToSearchTerm(),
            ToHierarchy(localTrack));
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult localTrack) =>
        localTrack.ArtistId is null && localTrack.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack.ArtistId, localTrack.AlbumId);
}
