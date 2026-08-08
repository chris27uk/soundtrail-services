using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes
{
    internal sealed class StorePlaylistTracksReadModelPortFake(
        IClockPort clock,
        StoreArtistCatalogReadModelPortFake artistCatalog,
        StoreDiscoveryFeedbackPortFake discoveryFeedback) : IStorePlaylistTracksReadModelPort
    {
        private readonly Dictionary<PlaylistId, (TrackId[] TrackIds, DateTimeOffset UpdatedAt)> playlists = [];

        public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
        {
            var existing = this.playlists.GetValueOrDefault(@event.PlaylistId).TrackIds ?? [];
            this.playlists[@event.PlaylistId] = (
                existing.Concat(@event.Tracks).Distinct().ToArray(),
                @event.ObservedAt);
            return Task.CompletedTask;
        }

        public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<GetTracksForPlaylistResponse?> ReadAsync(PlaylistId playlistId, CancellationToken cancellationToken)
        {
            if (!this.playlists.TryGetValue(playlistId, out var playlist))
            {
                return Task.FromResult<GetTracksForPlaylistResponse?>(null);
            }

            var tracks = playlist.TrackIds
                .Select(SelectPreferredTrack)
                .Where(static track => track is not null)
                .Select(static track => track!.Value)
                .Select(static track => new GetTracksForPlaylistTrackResponse(
                    track.Track.TrackId,
                    track.Track.Title,
                    track.Track.ArtistName,
                    track.Track.AlbumTitle,
                    track.Track.DurationMs,
                    track.Track.Isrc,
                    track.Track.ReleaseDate,
                    track.Track.ArtworkUrl,
                    track.Track.StreamingLocations.Length > 0,
                    track.Track.StreamingLocations
                        .Select(static location => new StreamingLocationResponse(
                            location.Provider.StableValue,
                            location.ExternalId,
                            location.Url))
                        .ToArray()))
                .ToArray();

            return Task.FromResult<GetTracksForPlaylistResponse?>(new GetTracksForPlaylistResponse(
                playlistId,
                tracks,
                ReadDiscovery(playlistId, tracks)));
        }

        private (ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)? SelectPreferredTrack(TrackId requestedTrackId)
        {
            var requested = TrackIdIndexProjection.From(requestedTrackId);
            return artistCatalog.Tracks
                .Select(track => (Stored: track, Projection: TrackIdIndexProjection.From(track.Track.TrackId)))
                .Where(track => track.Projection.SharesBaseWith(requested))
                .OrderBy(track => track.Projection.GetDistanceTo(requested))
                .ThenByDescending(static track => track.Stored.UpdatedAt)
                .Select(static track => ((ArtistId, ArtistCatalogTrackReadModel, DateTimeOffset)?)track.Stored)
                .FirstOrDefault();
        }

        private DiscoveryFeedbackResponse? ReadDiscovery(
            PlaylistId playlistId,
            IReadOnlyList<GetTracksForPlaylistTrackResponse> tracks)
        {
            var playlistDiscovery = discoveryFeedback.Read($"child_tracks_for_playlist:{playlistId.Value}");
            if (playlistDiscovery is null)
            {
                return null;
            }

            foreach (var track in tracks.Where(static track => !track.Playable))
            {
                var streamingDiscovery = discoveryFeedback.Read($"streaming_location_for_track:{track.TrackId.Value}");
                if (streamingDiscovery is not null && streamingDiscovery.Status is "requested" or "scheduled" or "deferred")
                {
                    return streamingDiscovery;
                }

                if (streamingDiscovery is null)
                {
                    return playlistDiscovery with
                    {
                        Status = "scheduled",
                        NextEligibleAt = clock.UtcNow.AddSeconds(15),
                        EarliestExpectedCompletionAt = clock.UtcNow.AddSeconds(75),
                        Reason = "Track streaming projection is still catching up.",
                        UpdatedAtUtc = clock.UtcNow
                    };
                }
            }

            return playlistDiscovery;
        }
    }
}
