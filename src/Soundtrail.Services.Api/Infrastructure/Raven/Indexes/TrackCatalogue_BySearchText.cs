using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class TrackCatalogue_BySearchText : AbstractIndexCreationTask<RavenTrackRecordDto>
{
    public TrackCatalogue_BySearchText()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.SearchText,
                            track.Isrc,
                            track.AppleId,
                            track.Mbid,
                            track.SpotifyId,
                            track.DurationMs
                        };

        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
