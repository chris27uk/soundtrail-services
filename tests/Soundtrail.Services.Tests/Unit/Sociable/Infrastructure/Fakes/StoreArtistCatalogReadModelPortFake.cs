using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes
{
    internal sealed class StoreArtistCatalogReadModelPortFake(
        ReadTrackForLookupPortFake readTrackForLookup) : IStoreArtistCatalogReadModelPort
    {
        private readonly Dictionary<TrackId, (ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)> tracks = [];

        public IReadOnlyCollection<(ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)> Tracks => this.tracks.Values;

        public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
        {
            foreach (var track in readModel.Tracks)
            {
                this.tracks[track.TrackId] = (readModel.ArtistId, track, readModel.UpdatedAt);
                readTrackForLookup.WithTrack(new TrackLookupContext(
                    readModel.ArtistId,
                    track.TrackId,
                    track.Title,
                    track.ArtistName,
                    track.Isrc));
            }

            return Task.CompletedTask;
        }

        public TrackLookupContext? Read(TrackId trackId) =>
            this.tracks.TryGetValue(trackId, out var stored)
                ? new TrackLookupContext(stored.ArtistId, trackId, stored.Track.Title, stored.Track.ArtistName, stored.Track.Isrc)
                : null;
    }
}
