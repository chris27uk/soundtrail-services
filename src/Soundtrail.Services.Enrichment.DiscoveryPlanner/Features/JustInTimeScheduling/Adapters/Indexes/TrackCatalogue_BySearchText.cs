using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Indexes;

internal sealed class TrackCatalogue_BySearchText : AbstractIndexCreationTask<RavenTrackRecordDto>
{
    public TrackCatalogue_BySearchText()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.SearchText,
                            track.NormalizedArtist,
                            track.NormalizedAlbumTitle,
                            track.NormalizedIsrc,
                            track.NormalizedMbid
                        };

        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
