using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

internal static class AlbumTracks
{
    public static AlbumId DefaultAlbumId => AlbumId.From("artist-1401", "album-1501");

    public static TrackId DefaultTrackId => TestTrackIds.Create("track-1601");

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
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    artistName,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);
    }
}
