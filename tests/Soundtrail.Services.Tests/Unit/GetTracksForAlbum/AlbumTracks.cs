using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

internal static class AlbumTracks
{
    public static AlbumId DefaultAlbumId => AlbumId.From("artist-1401", "album-1501");

    public static TrackId DefaultTrackId => TrackId.From("track-1601");

    public static GetTracksForAlbumResponse CreateResponse(
        AlbumId? albumId = null,
        string albumTitle = "The Album",
        TrackId? trackId = null,
        string title = "The Track",
        string artistName = "The Artist",
        int? durationMs = 201000,
        string? isrc = "GBAYE2401601",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-1601.jpg")
    {
        var resolvedAlbumId = albumId ?? DefaultAlbumId;
        var resolvedTrackId = trackId ?? DefaultTrackId;

        return new GetTracksForAlbumResponse(
            ArtistId.From(resolvedAlbumId.ArtistId),
            resolvedAlbumId,
            albumTitle,
            [
                new GetTracksForAlbumTrackResponse(
                    resolvedTrackId,
                    new MusicCatalogId.Track(resolvedTrackId),
                    title,
                    artistName,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);
    }
}
