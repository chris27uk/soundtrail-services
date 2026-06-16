namespace Soundtrail.Domain.Catalog;

public sealed record CatalogTrackHierarchy(
    ArtistId? ArtistId,
    AlbumId? AlbumId);
