using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;

public sealed record GetTracksForArtistResponseDto(
    string ArtistId,
    string ArtistName,
    GetTracksForArtistTrackResponseDto[] Tracks,
    DiscoveryFeedbackResponseDto? Discovery);

public sealed record GetTracksForArtistTrackResponseDto(
    string TrackId,
    string MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
