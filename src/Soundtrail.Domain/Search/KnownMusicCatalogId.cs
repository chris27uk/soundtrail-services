using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Search
{
    public class KnownMusicCatalogId
    {
        private KnownMusicCatalogId(ArtistId? artistId, AlbumId? albumId, TrackId? trackId)
        {
            ArtistId = artistId;
            AlbumId = albumId;
            TrackId = trackId;
        }
        
        public static KnownMusicCatalogId FromArtistId(ArtistId artistId) => new(artistId, null, null);
        public static KnownMusicCatalogId FromAlbumId(AlbumId albumId) => new(null, albumId, null);
        public static KnownMusicCatalogId FromTrackId(TrackId trackId) => new(null, null, trackId);
        
        public ArtistId? ArtistId { get; init; }
        public AlbumId? AlbumId { get; init; }
        public TrackId? TrackId { get; init; }
    }
}
