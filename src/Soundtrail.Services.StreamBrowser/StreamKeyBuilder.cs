using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.StreamBrowser;

internal static class StreamKeyBuilder
{
    public static IReadOnlyList<KeyingTemplate> Templates { get; } =
    [
        new(
            StreamKinds.Work,
            "search",
            "Search work",
            "catalog-stream work keyed by a normalised search query",
            [
                new KeyingField("query", "Search query", "Midnight Signals Aurora Lane", true)
            ],
            "search:{normalized query}"),
        new(
            StreamKinds.Work,
            "child_albums_for_artist",
            "Child albums for artist",
            "Work stream for discovering albums belonging to an artist",
            [new KeyingField("artistId", "Artist id", "musicbrainz-artist:nirvana", true)],
            "child_albums_for_artist:{artistId}"),
        new(
            StreamKinds.Work,
            "child_tracks_for_artist",
            "Child tracks for artist",
            "Work stream for discovering tracks belonging to an artist",
            [new KeyingField("artistId", "Artist id", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", true)],
            "child_tracks_for_artist:{artistId}"),
        new(
            StreamKinds.Work,
            "child_tracks_for_album",
            "Child tracks for album",
            "Work stream for discovering tracks on an album (artistId:albumId)",
            [
                new KeyingField("artistId", "Artist id", "musicbrainz-artist:nirvana", true),
                new KeyingField("albumId", "Album id", "nevermind", true)
            ],
            "child_tracks_for_album:{artistId}:{albumId}"),
        new(
            StreamKinds.Work,
            "child_tracks_for_playlist",
            "Child tracks for playlist",
            "Work stream for playlist track discovery. Prefer playlist name (compact-normalised) or paste an existing playlist id.",
            [
                new KeyingField("playlistName", "Playlist name", "Worldwide Song Chart", false),
                new KeyingField("playlistId", "Playlist id (optional override)", "worldwidesongchart", false)
            ],
            "child_tracks_for_playlist:{playlistId}"),
        new(
            StreamKinds.Work,
            "streaming_location_for_track",
            "Streaming location for track",
            "Work stream for resolving streaming links for a packed track id",
            [new KeyingField("trackId", "Track id", "trk2_…", true)],
            "streaming_location_for_track:{trackId}"),
        new(
            StreamKinds.Catalog,
            "artist",
            "Artist catalog",
            "Artist catalog stream keyed directly by ArtistId",
            [new KeyingField("artistId", "Artist id", "musicbrainz-artist:nirvana", true)],
            "{artistId}")
    ];

    public static StreamKeyResult Build(string templateId, IReadOnlyDictionary<string, string?> values)
    {
        var template = Templates.FirstOrDefault(x => x.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown keying template '{templateId}'.", nameof(templateId));

        var streamId = template.Id switch
        {
            "search" => new SearchCriteria(Require(values, "query")).NormalisedIdentifier,
            "child_albums_for_artist" => new CatalogItemOperation.ChildAlbumsForArtist(
                ArtistId.From(Require(values, "artistId"))).StableIdentifier(),
            "child_tracks_for_artist" => new CatalogItemOperation.ChildTracksForArtist(
                ArtistId.From(Require(values, "artistId"))).StableIdentifier(),
            "child_tracks_for_album" => new CatalogItemOperation.ChildTracksForAlbum(
                AlbumId.From(Require(values, "artistId"), Require(values, "albumId"))).StableIdentifier(),
            "child_tracks_for_playlist" => BuildPlaylistKey(values),
            "streaming_location_for_track" => new CatalogItemOperation.StreamingLocationForTrack(
                TrackId.From(Require(values, "trackId"))).StableIdentifier(),
            "artist" => ArtistId.From(Require(values, "artistId")).StableValue,
            _ => throw new ArgumentException($"Unsupported keying template '{templateId}'.", nameof(templateId))
        };

        return new StreamKeyResult(
            template.Kind,
            StreamKinds.AggregateType(template.Kind),
            streamId,
            $"{StreamKinds.AggregateType(template.Kind)}-streams/{streamId}",
            StreamKinds.EventPrefix(template.Kind, streamId));
    }

    private static string BuildPlaylistKey(IReadOnlyDictionary<string, string?> values)
    {
        if (values.TryGetValue("playlistId", out var playlistId) && !string.IsNullOrWhiteSpace(playlistId))
        {
            return $"child_tracks_for_playlist:{playlistId.Trim()}";
        }

        var playlistName = Require(values, "playlistName");
        return new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName(playlistName))
            .StableIdentifier();
    }

    private static string Require(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Field '{key}' is required.");
        }

        return value.Trim();
    }
}

internal sealed record KeyingField(string Name, string Label, string Placeholder, bool IsRequired);

internal sealed record KeyingTemplate(
    string Kind,
    string Id,
    string Title,
    string Description,
    IReadOnlyList<KeyingField> Fields,
    string Pattern);

internal sealed record StreamKeyResult(
    string Kind,
    string AggregateType,
    string StreamId,
    string MetadataDocumentId,
    string EventDocumentPrefix);
