namespace Soundtrail.Services.Api.Features.GetTrack.Adapters;

public sealed record GetTrackResponseDto(
    string TrackId,
    string MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
