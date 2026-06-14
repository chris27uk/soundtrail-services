using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Albums_ByArtistAndName : AbstractIndexCreationTask<CatalogAlbumDocument>
{
    public Albums_ByArtistAndName()
    {
        Map = albums => from album in albums
                        select new
                        {
                            album.ArtistId,
                            album.NormalizedName
                        };
    }
}
