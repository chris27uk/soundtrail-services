using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Search_Artists : AbstractIndexCreationTask<CatalogArtistRecordDto>
{
    public Search_Artists()
    {
        Map = artists => from artist in artists
                         select new
                         {
                             artist.ArtistId,
                             artist.Name,
                             artist.NormalizedName,
                             artist.SearchText,
                             artist.MusicBrainzArtistId,
                             artist.AvailableProviders,
                             artist.TerminallyUnavailableProviders
                         };

        Index(x => x.Name, FieldIndexing.Search);
        Index(x => x.NormalizedName, FieldIndexing.Search);
        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
