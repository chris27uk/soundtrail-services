using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Indexes;

internal sealed class Tracks_ByMusicBrainzRecordingId : AbstractIndexCreationTask<CatalogTrackRecordDto>
{
    public Tracks_ByMusicBrainzRecordingId()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.MusicBrainzRecordingId
                        };
    }
}
