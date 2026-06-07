using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Indexes;

internal sealed class TrackCatalogue_BySearchText : AbstractIndexCreationTask<RavenTrackDocument>
{
    public TrackCatalogue_BySearchText()
    {
        Map = tracks => from track in tracks
                        select new
                        {
                            track.SearchText
                        };

        Index(x => x.SearchText, FieldIndexing.Search);
    }
}
