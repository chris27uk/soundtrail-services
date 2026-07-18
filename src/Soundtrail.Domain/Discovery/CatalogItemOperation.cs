using Dunet;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record CatalogItemOperation
    {
        public partial record StreamingLocationForTrack(TrackId Id);
    
        public partial record ChildAlbumsForArtist(ArtistId Id);
    
        public partial record ChildTracksForArtist(ArtistId Id);
    
        public partial record ChildTracksForAlbum(AlbumId Id);
    
        public partial record ChildTracksForPlaylist(PlaylistId Id);

        public string StableIdentifier()
        {
            return this switch
            {
                ChildAlbumsForArtist (var artistId) => $"child_albums_for_artist:{artistId.Value}",
                ChildTracksForArtist (var artistId) => $"child_tracks_for_artist:{artistId.Value}",
                ChildTracksForAlbum (var albumId) => $"child_tracks_for_album:{albumId.StableValue}",
                StreamingLocationForTrack (var trackId) => $"streaming_location_for_track:{trackId.Value}",
                _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{GetType().Name}'.")
            };
        }
    }
}
