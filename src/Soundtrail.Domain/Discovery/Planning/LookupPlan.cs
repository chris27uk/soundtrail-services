using Dunet;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Planning;

public sealed record LookupPlan(IReadOnlyList<PlannedLookup> Lookups);

[Union]
public partial record PlannedLookup
{
    public partial record MusicbrainzSearch(
        SearchCriteria SearchCriteria,
        LookupPriorityBand Priority);

    public partial record MusicbrainzArtistAlbums(
        ArtistId ArtistId,
        LookupPriorityBand Priority);

    public partial record MusicbrainzArtistTracks(
        ArtistId ArtistId,
        LookupPriorityBand Priority);

    public partial record MusicbrainzAlbumTracks(
        AlbumId AlbumId,
        LookupPriorityBand Priority);

    public partial record StreamingLocationByIsrc(
        TrackId TrackId,
        ProviderName Provider,
        LookupPriorityBand Priority);

    public partial record StreamingLocationByTrackMetadata(
        TrackId TrackId,
        ProviderName Provider,
        LookupPriorityBand Priority);

    public partial record PlaylistTracksByProvider(
        PlaylistId PlaylistId,
        ProviderName Provider,
        LookupPriorityBand Priority);
}
