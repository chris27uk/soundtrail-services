using Raven.Client.Documents.Indexes;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters.Indexes;

internal sealed class TrackCatalogue_BySearchText : AbstractIndexCreationTask<RavenTrackRecordDto>
{
    public TrackCatalogue_BySearchText()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            SearchText = string.IsNullOrWhiteSpace(track.NormalizedAlbumTitle)
                                ? track.SearchText
                                : track.SearchText + " " + track.NormalizedAlbumTitle,
                            track.NormalizedArtist,
                            track.NormalizedAlbumTitle,
                            track.NormalizedIsrc,
                            track.NormalizedMbid,
                            track.ReleaseDate
                        };

        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
