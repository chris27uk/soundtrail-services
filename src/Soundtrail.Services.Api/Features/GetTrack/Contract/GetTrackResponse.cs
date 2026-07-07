using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetTrack.Contract;

public sealed record GetTrackResponse(
    TrackId TrackId,
    MusicCatalogId MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
