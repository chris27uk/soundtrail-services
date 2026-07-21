using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;

public sealed class ArtistCatalogChangedProjectorHandler(
    IEventStreamRepository<ArtistId> repository,
    IStoreArtistCatalogReadModelPort storeArtistCatalogReadModelPort)
{
    public async Task Handle(ArtistId artistId, CancellationToken cancellationToken = default)
    {
        var stream = await repository.LoadAsync(artistId, cancellationToken);
        var snapshot = ArtistCatalogReadModelBuilder.Build(stream.Events);
        await storeArtistCatalogReadModelPort.StoreAsync(ToReadModel(artistId, snapshot), cancellationToken);
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
                    album.SourceAlbumId,
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
                    track.ArtworkUrl))
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
                        snapshot.Tracks[trackDiscovered.Track.TrackId.Value] = trackDiscovered.Track;
                        snapshot.UpdatedAt = trackDiscovered.ObservedAt;
                        break;

                    case StreamingLocationDiscovered streamingLocationDiscovered:
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
                album.UpdatedAt = artworkDiscovered.ObservedAt;
            }

            snapshot.UpdatedAt = artworkDiscovered.ObservedAt;
        }
    }
}
