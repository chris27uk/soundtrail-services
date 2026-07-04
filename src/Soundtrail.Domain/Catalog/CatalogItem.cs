using Dunet;

namespace Soundtrail.Domain.Catalog;

[Union]
public partial record CatalogItem
{
    public partial record MusicArtist(Artist Artist);
    
    public partial record MusicAlbum(Album Album);

    public partial record MusicTrack(Track Track);
}
