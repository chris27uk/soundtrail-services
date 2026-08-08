using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
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
