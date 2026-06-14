using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Artists_ByMusicBrainzId : AbstractIndexCreationTask<CatalogArtistRecordDto>
{
    public Artists_ByMusicBrainzId()
    {
        Map = artists => from artist in artists
                         select new
                         {
                             artist.MusicBrainzArtistId
                         };
    }
}
