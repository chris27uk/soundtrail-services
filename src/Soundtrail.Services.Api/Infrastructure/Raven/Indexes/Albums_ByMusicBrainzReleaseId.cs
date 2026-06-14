using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Albums_ByMusicBrainzReleaseId : AbstractIndexCreationTask<CatalogAlbumDocument>
{
    public Albums_ByMusicBrainzReleaseId()
    {
        Map = albums => from album in albums
                        select new
                        {
                            album.MusicBrainzReleaseId
                        };
    }
}
