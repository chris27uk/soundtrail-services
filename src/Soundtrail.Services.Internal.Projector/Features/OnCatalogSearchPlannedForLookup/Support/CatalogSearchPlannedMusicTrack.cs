namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

public sealed record CatalogSearchPlannedMusicTrack(
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
