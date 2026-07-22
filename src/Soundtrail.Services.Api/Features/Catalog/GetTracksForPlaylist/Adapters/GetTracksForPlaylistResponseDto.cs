using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

public sealed record GetTracksForPlaylistResponseDto(
    string PlaylistId,
    GetTracksForPlaylistTrackResponseDto[] Tracks,
    DiscoveryFeedbackResponseDto? Discovery);

public sealed record GetTracksForPlaylistTrackResponseDto(
    string TrackId,
    string MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
