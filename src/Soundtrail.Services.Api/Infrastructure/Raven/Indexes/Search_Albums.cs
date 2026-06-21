using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Search_Albums : AbstractIndexCreationTask<CatalogAlbumRecordDto>
{
    public Search_Albums()
    {
        Map = albums => from album in albums
                        select new
                        {
                            album.AlbumId,
                            album.ArtistId,
                            album.Name,
                            album.NormalizedName,
                            album.ArtistName,
                            album.SearchText,
                            album.MusicBrainzReleaseId,
                            album.AvailableProviders,
                            album.TerminallyUnavailableProviders
                        };

        Index(x => x.Name, FieldIndexing.Search);
        Index(x => x.NormalizedName, FieldIndexing.Search);
        Index(x => x.ArtistName, FieldIndexing.Search);
        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
