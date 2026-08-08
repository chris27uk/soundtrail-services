using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes
{
    internal sealed class StoreArtistCatalogReadModelPortFake(
        ReadTrackForLookupPortFake readTrackForLookup) : IStoreArtistCatalogReadModelPort
    {
        private readonly Dictionary<ArtistId, ArtistCatalogReadModel> artists = [];

        public IReadOnlyCollection<(ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)> Tracks =>
            artists.Values
                .SelectMany(static readModel => readModel.Tracks.Select(track => (
                    readModel.ArtistId,
                    track,
                    readModel.UpdatedAt)))
                .ToArray();

        public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
        {
            artists[readModel.ArtistId] = readModel;
            foreach (var track in readModel.Tracks)
            {
                readTrackForLookup.WithTrack(new TrackLookupContext(
                    readModel.ArtistId,
                    track.TrackId,
                    track.Title,
                    track.ArtistName,
                    track.Isrc));
            }

            return Task.CompletedTask;
        }

        public Task<GetTracksForArtistResponse?> ReadAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            if (!artists.TryGetValue(artistId, out var readModel))
            {
                return Task.FromResult<GetTracksForArtistResponse?>(null);
            }

            return Task.FromResult<GetTracksForArtistResponse?>(new GetTracksForArtistResponse(
                readModel.ArtistId,
                ArtistName.From(readModel.ArtistName),
                readModel.Tracks
                    .Select(static track => new GetTracksForArtistTrackResponse(
                        track.TrackId,
                        track.Title,
                        track.ArtistName,
                        track.AlbumTitle,
                        track.DurationMs,
                        track.Isrc,
                        track.ReleaseDate,
                        track.ArtworkUrl,
                        track.StreamingLocations.Length > 0,
                        track.StreamingLocations
                            .Select(static location => new StreamingLocationResponse(
                                location.Provider.StableValue,
                                location.ExternalId,
                                location.Url))
                            .ToArray()))
                    .ToArray()));
        }

        public Task<GetAlbumsForArtistResponse?> ReadAlbumsAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            if (!artists.TryGetValue(artistId, out var readModel) || readModel.Albums.Length == 0)
            {
                return Task.FromResult<GetAlbumsForArtistResponse?>(null);
            }

            return Task.FromResult<GetAlbumsForArtistResponse?>(new GetAlbumsForArtistResponse(
                readModel.ArtistId,
                ArtistName.From(readModel.ArtistName),
                readModel.Albums
                    .Select(static album => new GetAlbumsForArtistAlbumResponse(
                        album.AlbumId,
                        new CatalogItemId.Album(album.AlbumId),
                        album.AlbumTitle,
                        album.ReleaseDate,
                        album.ArtworkUrl))
                    .ToArray()));
        }

        public Task<GetTracksForAlbumResponse?> ReadAlbumTracksAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            var artistId = ArtistId.From(albumId.ArtistId);
            if (!artists.TryGetValue(artistId, out var readModel))
            {
                return Task.FromResult<GetTracksForAlbumResponse?>(null);
            }

            var albumTracks = readModel.Tracks
                .Where(track => string.Equals(track.AlbumId, albumId.StableValue, StringComparison.Ordinal))
                .ToArray();
            if (albumTracks.Length == 0)
            {
                return Task.FromResult<GetTracksForAlbumResponse?>(null);
            }

            var albumTitle = readModel.Albums
                .FirstOrDefault(album => album.AlbumId == albumId)
                ?.AlbumTitle
                ?? albumTracks.Select(static track => track.AlbumTitle).FirstOrDefault(static title => !string.IsNullOrWhiteSpace(title))
                ?? string.Empty;

            return Task.FromResult<GetTracksForAlbumResponse?>(new GetTracksForAlbumResponse(
                artistId,
                albumId,
                albumTitle,
                albumTracks
                    .Select(static track => new GetTracksForAlbumTrackResponse(
                        track.TrackId,
                        track.Title,
                        track.ArtistName,
                        track.DurationMs,
                        track.Isrc,
                        track.ReleaseDate,
                        track.ArtworkUrl,
                        track.StreamingLocations.Length > 0,
                        track.StreamingLocations
                            .Select(static location => new StreamingLocationResponse(
                                location.Provider.StableValue,
                                location.ExternalId,
                                location.Url))
                            .ToArray()))
                    .ToArray()));
        }

        public TrackLookupContext? Read(TrackId trackId) =>
            Tracks
                .Where(stored => stored.Track.TrackId == trackId)
                .Select(stored => new TrackLookupContext(
                    stored.ArtistId,
                    trackId,
                    stored.Track.Title,
                    stored.Track.ArtistName,
                    stored.Track.Isrc))
                .FirstOrDefault();
    }
}
