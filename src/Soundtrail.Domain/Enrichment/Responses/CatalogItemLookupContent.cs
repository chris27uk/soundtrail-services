using Dunet;

namespace Soundtrail.Domain.Enrichment.Responses;

[Union]
public partial record CatalogItemLookupContent
{
    public partial record Track(MusicCatalogMetadataFetched Value);

    public partial record Artist(ArtistMetadataFetched Value);

    public partial record Album(AlbumMetadataFetched Value);
}
