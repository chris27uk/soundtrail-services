using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTrack
{
    internal static class Tracks
    {
        public static TrackId DefaultTrackId => TrackId.From("track-201");

        public static GetTrackResponse CreateTrackResponse(
            TrackId? trackId = null,
            string title = "The Track",
            string artistName = "The Artist",
            string? albumTitle = "The Album",
            int? durationMs = 201000,
            string? isrc = "GBAYE2400301",
            DateOnly? releaseDate = null,
            string? artworkUrl = "https://cdn.soundtrail.test/tracks/mc_track_201.jpg")
        {
            return new GetTrackResponse(
                trackId ?? DefaultTrackId,
                new MusicCatalogId.Track(trackId ?? DefaultTrackId),
                title,
                artistName,
                albumTitle,
                durationMs,
                isrc,
                releaseDate ?? new DateOnly(2024, 1, 2),
                artworkUrl);
        }
    }
}
