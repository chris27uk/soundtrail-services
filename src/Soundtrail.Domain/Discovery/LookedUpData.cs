using Dunet;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record LookedUpData
    {
        public partial record CatalogEntries(IReadOnlyList<CatalogDiscoveryEntry> Values);

        public partial record PlaylistTrackReferences(IReadOnlyList<TrackReference> Values);

        public partial record TrackStreamingLink(
            ArtistId ArtistId,
            TrackId TrackId,
            StreamingLocation Value);
    }
}
