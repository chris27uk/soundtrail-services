using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Tracks_ByArtistAlbumAndName : AbstractIndexCreationTask<CatalogTrackRecordDto>
{
    public Tracks_ByArtistAlbumAndName()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.ArtistId,
                            track.AlbumId,
                            track.NormalizedTitle
                        };
    }
}
