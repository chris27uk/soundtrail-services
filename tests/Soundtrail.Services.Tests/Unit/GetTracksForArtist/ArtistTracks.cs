using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForArtist;

internal static class ArtistTracks
{
    public static ArtistId DefaultArtistId => ArtistId.From("artist-2301");

    public static TrackId DefaultTrackId => TrackId.From("track-2401");

    public static GetTracksForArtistResponse CreateResponse(
        ArtistId? artistId = null,
        string artistName = "The Artist",
        TrackId? trackId = null,
        string title = "The Track",
        string trackArtistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2402401",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-2401.jpg")
    {
        var resolvedArtistId = artistId ?? DefaultArtistId;
        var resolvedTrackId = trackId ?? DefaultTrackId;

        return new GetTracksForArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            [
                new GetTracksForArtistTrackResponse(
                    resolvedTrackId,
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    trackArtistName,
                    albumTitle,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);
    }
}
