using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;

/// <summary>
/// Rebuilds artist-catalog read models from the artist event stream.
/// Invoked by <c>CatalogItemChangedProjectorHandler</c> only after the triggering
/// catalog-stream event has been appended, so Scrutor handler order cannot project
/// a stale stream (which left Midnight Signals without Spotify in CI).
/// </summary>
public sealed class ArtistCatalogChangedProjectorHandler(
    IEventStreamRepository<ArtistId> repository,
    IStoreArtistCatalogReadModelPort storeArtistCatalogReadModelPort,
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort)
{
    public async Task Handle(ArtistId artistId, CancellationToken cancellationToken = default)
    {
        var stream = await repository.LoadAsync(artistId, cancellationToken);
        var snapshot = ArtistCatalogReadModelBuilder.Build(stream.Events);
        var readModel = ToReadModel(artistId, snapshot);
        await storeArtistCatalogReadModelPort.StoreAsync(readModel, cancellationToken);

        foreach (var track in readModel.Tracks)
        {
            await storePlaylistTracksReadModelPort.RepairTrackAsync(track.TrackId, cancellationToken);
        }
    }

    private static ArtistCatalogReadModel ToReadModel(ArtistId artistId, ArtistCatalogSnapshot snapshot) =>
        new(
            artistId,
            snapshot.ArtistName ?? string.Empty,
            snapshot.ArtworkUrl,
            snapshot.UpdatedAt,
            snapshot.Albums.Values
                .Select(album => new ArtistCatalogAlbumReadModel(
                    album.AlbumId,
                    album.AlbumTitle ?? string.Empty,
                    SourceSystemIdSet.MusicBrainzIdOrNull(album.SourceSystemIds),
                    album.ReleaseDate,
                    album.ArtworkUrl))
                .ToArray(),
            snapshot.Tracks.Values
                .Select(track => new ArtistCatalogTrackReadModel(
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
                        .Select(static location => new ArtistCatalogStreamingLocationReadModel(
                            location.Provider,
                            location.ExternalId,
                            location.Url.ToString()))
                        .ToArray()))
                .ToArray());

    private sealed class ArtistCatalogSnapshot
    {
        public string? ArtistName { get; set; }

        public string? ArtworkUrl { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Dictionary<string, Album> Albums { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Track> Tracks { get; } = new(StringComparer.Ordinal);
    }

    private static class ArtistCatalogReadModelBuilder
    {
        public static ArtistCatalogSnapshot Build(IReadOnlyList<IDomainEvent> events)
        {
            var snapshot = new ArtistCatalogSnapshot();

            foreach (var @event in events)
            {
                switch (@event)
                {
                    case ArtistDiscovered artistDiscovered:
                        snapshot.ArtistName = artistDiscovered.Artist.Name.Value;
                        snapshot.ArtworkUrl = artistDiscovered.Artist.ImageUrl;
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

        private static void ApplyTrackDiscovered(
            ArtistCatalogSnapshot snapshot,
            TrackDiscovered trackDiscovered)
        {
            var incoming = trackDiscovered.Track;
            if (!snapshot.Tracks.TryGetValue(incoming.TrackId.Value, out var track))
            {
                snapshot.Tracks[incoming.TrackId.Value] = incoming;
                return;
            }

            // Preserve provider references if StreamingLocationDiscovered arrived first.
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

        private static void ApplyStreamingLocation(
            ArtistCatalogSnapshot snapshot,
            StreamingLocationDiscovered streamingLocationDiscovered)
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

        private static void UpdateTrackArtwork(ArtistCatalogSnapshot snapshot, TrackId trackId, ArtworkDiscovered artworkDiscovered)
        {
            if (snapshot.Tracks.TryGetValue(trackId.Value, out var track))
            {
                track.ArtworkUrl = artworkDiscovered.Url.ToString();
                track.UpdatedAt = artworkDiscovered.ObservedAt;
            }

            snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
        }

        private static void UpdateAlbumArtwork(ArtistCatalogSnapshot snapshot, AlbumId albumId, ArtworkDiscovered artworkDiscovered)
        {
            if (snapshot.Albums.TryGetValue(albumId.StableValue, out var album))
            {
                album.ArtworkUrl = artworkDiscovered.Url.ToString();
            }

            snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
        }
    }
}
