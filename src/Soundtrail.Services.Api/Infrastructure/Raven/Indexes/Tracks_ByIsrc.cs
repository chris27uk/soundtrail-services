using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Tracks_ByIsrc : AbstractIndexCreationTask<CatalogTrackDocument>
{
    public Tracks_ByIsrc()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.Isrc
                        };
    }
}
