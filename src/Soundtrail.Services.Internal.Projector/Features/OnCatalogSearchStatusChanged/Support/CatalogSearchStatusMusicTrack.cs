namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

public sealed record CatalogSearchStatusMusicTrack(
    string? ArtistId,
    string? AlbumId,
    bool IsPlayable,
    string? Isrc,
    string? ResolvedIsrc,
    string? Title,
    string? ResolvedTitle,
    string? Artist,
    string? ResolvedArtist,
    string? AlbumTitle);
