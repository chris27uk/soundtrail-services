using Dunet;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Planning;

public sealed record LookupPlan(IReadOnlyList<LookupIntent> Intents)
{
    public IReadOnlyList<LookupAttempt> Attempts =>
        Intents.SelectMany(static intent => intent.Attempts()).ToArray();
}

[Union]
public partial record LookupIntent
{
    public partial record SearchCatalogItems(
        SearchCriteria SearchCriteria,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);

    public partial record ArtistAlbums(
        ArtistId ArtistId,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);

    public partial record ArtistTracks(
        ArtistId ArtistId,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);

    public partial record AlbumTracks(
        AlbumId AlbumId,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);

    public partial record StreamingLocation(
        TrackId TrackId,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);

    public partial record PlaylistTracks(
        PlaylistId PlaylistId,
        LookupPriorityBand Priority,
        IReadOnlyList<LookupAttempt> Attempts);
}

public static class LookupIntentExtensions
{
    public static IReadOnlyList<LookupAttempt> Attempts(this LookupIntent intent) =>
        intent.Match(
            search => search.Attempts,
            artistAlbums => artistAlbums.Attempts,
            artistTracks => artistTracks.Attempts,
            albumTracks => albumTracks.Attempts,
            streamingLocation => streamingLocation.Attempts,
            playlistTracks => playlistTracks.Attempts);
}

[Union]
public partial record LookupAttempt
{
    public partial record MusicbrainzSearchCatalogItems(
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
