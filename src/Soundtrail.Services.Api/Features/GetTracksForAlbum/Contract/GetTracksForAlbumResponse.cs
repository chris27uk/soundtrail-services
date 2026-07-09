using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

public sealed record GetTracksForAlbumResponse(
    ArtistId ArtistId,
    AlbumId AlbumId,
    string AlbumTitle,
    GetTracksForAlbumTrackResponse[] Tracks);

public sealed record GetTracksForAlbumTrackResponse(
    TrackId TrackId,
    MusicCatalogId MusicCatalogId,
    string Title,
    string ArtistName,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
