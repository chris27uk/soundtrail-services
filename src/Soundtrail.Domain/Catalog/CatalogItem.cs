using Dunet;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Playlist = Soundtrail.Domain.Catalog.Aggregates.Playlist;

namespace Soundtrail.Domain.Catalog;

[Union]
public partial record CatalogItem
{
    public partial record MusicArtist(Artist Artist);
    
    public partial record MusicAlbum(Album Album);

    public partial record MusicTrack(Track Track);
    
    public partial record MusicPlaylist(Playlist Playlist);
}
