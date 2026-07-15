using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

public sealed record GetTracksForAlbumResponse(
    ArtistId ArtistId,
    AlbumId AlbumId,
    string AlbumTitle,
    GetTracksForAlbumTrackResponse[] Tracks);

public sealed record GetTracksForAlbumTrackResponse(
    TrackId TrackId,
    CatalogItemId MusicCatalogId,
    string Title,
    string ArtistName,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
