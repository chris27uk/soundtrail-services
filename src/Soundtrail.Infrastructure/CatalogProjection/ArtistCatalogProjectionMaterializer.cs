using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Adapters.CatalogProjection;

public static class ArtistCatalogProjectionMaterializer
{
    public static ArtistCatalogProjection Build(ArtistId artistId, IReadOnlyList<IDomainEvent> events)
    {
        var snapshot = SnapshotBuilder.Build(events);
        return new ArtistCatalogProjection(
            artistId,
            snapshot.ArtistName ?? string.Empty,
            snapshot.ArtworkUrl,
            SourceSystemIdSet.MusicBrainzIdOrNull(snapshot.ArtistSourceSystemIds),
            snapshot.UpdatedAt,
            snapshot.Albums.Values
                .Select(album => new ArtistCatalogAlbumProjection(
                    album.AlbumId,
                    album.AlbumTitle ?? string.Empty,
                    SourceSystemIdSet.MusicBrainzIdOrNull(album.SourceSystemIds),
                    album.ReleaseDate,
                    album.ArtworkUrl))
                .ToArray(),
            snapshot.Tracks.Values
                .Select(track => new ArtistCatalogTrackProjection(
                    track.TrackId,
                    track.Title,
                    track.ArtistName,
                    track.AlbumId,
                    track.AlbumTitle,
                    track.DurationMs,
                    track.Isrc,
                    track.ReleaseDate,
                    track.ReleaseType,
                    track.ArtworkUrl,
                    track.ProviderReferences.Values
                        .OrderBy(static location => location.Provider.Value, StringComparer.Ordinal)
                        .Select(static location => new ArtistCatalogStreamingLocationProjection(
                            location.Provider,
                            location.ExternalId,
                            location.Url.ToString()))
                        .ToArray()))
                .ToArray());
    }

    private sealed class Snapshot
    {
        public string? ArtistName { get; set; }

        public string? ArtworkUrl { get; set; }

        public HashSet<SourceSystemId> ArtistSourceSystemIds { get; } = [];

        public DateTimeOffset UpdatedAt { get; set; }

        public Dictionary<string, Album> Albums { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Track> Tracks { get; } = new(StringComparer.Ordinal);
    }

    private static class SnapshotBuilder
    {
        public static Snapshot Build(IReadOnlyList<IDomainEvent> events)
        {
            var snapshot = new Snapshot();

            foreach (var @event in events)
            {
                switch (@event)
                {
                    case ArtistDiscovered artistDiscovered:
                        snapshot.ArtistName = artistDiscovered.Artist.Name.Value;
                        snapshot.ArtworkUrl = artistDiscovered.Artist.ImageUrl;
                        SourceSystemIdSet.UnionWith(snapshot.ArtistSourceSystemIds, artistDiscovered.Artist.SourceSystemIds);
                        snapshot.UpdatedAt = artistDiscovered.ObservedAt;
                        break;

                    case AlbumDiscovered albumDiscovered:
                        snapshot.Albums[albumDiscovered.Album.AlbumId.StableValue] = albumDiscovered.Album;
                        snapshot.UpdatedAt = albumDiscovered.ObservedAt;
                        break;

                    case TrackDiscovered trackDiscovered:
                        snapshot.ArtistName ??= trackDiscovered.Track.ArtistName;
                        ApplyTrackDiscovered(snapshot, trackDiscovered);
                        snapshot.UpdatedAt = trackDiscovered.ObservedAt;
                        break;

                    case StreamingLocationDiscovered streamingLocationDiscovered:
                        ApplyStreamingLocation(snapshot, streamingLocationDiscovered);
                        snapshot.UpdatedAt = streamingLocationDiscovered.ObservedAt;
                        break;

                    case ArtworkDiscovered artworkDiscovered:
                        artworkDiscovered.CatalogItemId.Match(
                            track => UpdateTrackArtwork(snapshot, track.Id, artworkDiscovered),
                            _ =>
                            {
                                snapshot.ArtworkUrl = artworkDiscovered.Url.ToString();
                                snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
                            },
                            album => UpdateAlbumArtwork(snapshot, album.Id, artworkDiscovered),
                            _ => { });
                        break;
                }
            }

            return snapshot;
        }

        private static void ApplyTrackDiscovered(Snapshot snapshot, TrackDiscovered trackDiscovered)
        {
            var incoming = trackDiscovered.Track;
            if (!snapshot.Tracks.TryGetValue(incoming.TrackId.Value, out var track))
            {
                snapshot.Tracks[incoming.TrackId.Value] = incoming;
                return;
            }

            track.Title = incoming.Title;
            track.ArtistName = incoming.ArtistName;
            track.AlbumId = incoming.AlbumId;
            track.AlbumTitle = incoming.AlbumTitle;
            track.DurationMs = incoming.DurationMs;
            track.Isrc = incoming.Isrc;
            SourceSystemIdSet.UnionWith(track.SourceSystemIds, incoming.SourceSystemIds);
            track.ReleaseDate = incoming.ReleaseDate;
            track.ReleaseType = incoming.ReleaseType;
            track.ArtworkUrl = incoming.ArtworkUrl ?? track.ArtworkUrl;
            track.StreamingLocationsRequired = incoming.StreamingLocationsRequired;
            track.UpdatedAt = trackDiscovered.ObservedAt;
        }

        private static void ApplyStreamingLocation(Snapshot snapshot, StreamingLocationDiscovered streamingLocationDiscovered)
        {
            var trackId = streamingLocationDiscovered.MusicCatalogId.AsTrack();
            if (!snapshot.Tracks.TryGetValue(trackId.Value, out var track))
            {
                track = new Track(trackId);
                snapshot.Tracks[trackId.Value] = track;
            }

            track.ProviderReferences[streamingLocationDiscovered.Provider.Value] = new StreamingLocation(
                streamingLocationDiscovered.Provider,
                streamingLocationDiscovered.ExternalId,
                streamingLocationDiscovered.Url,
                streamingLocationDiscovered.SourceProvider,
                streamingLocationDiscovered.ObservedAt);
            track.FailedProviders.Remove(streamingLocationDiscovered.Provider.Value);
            track.UpdatedAt = streamingLocationDiscovered.ObservedAt;
        }

        private static void UpdateTrackArtwork(Snapshot snapshot, TrackId trackId, ArtworkDiscovered artworkDiscovered)
        {
            if (snapshot.Tracks.TryGetValue(trackId.Value, out var track))
            {
                track.ArtworkUrl = artworkDiscovered.Url.ToString();
                track.UpdatedAt = artworkDiscovered.ObservedAt;
            }

            snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
        }

        private static void UpdateAlbumArtwork(Snapshot snapshot, AlbumId albumId, ArtworkDiscovered artworkDiscovered)
        {
            if (snapshot.Albums.TryGetValue(albumId.StableValue, out var album))
            {
                album.ArtworkUrl = artworkDiscovered.Url.ToString();
            }

            snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
        }
    }
}
