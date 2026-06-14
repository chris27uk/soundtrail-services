namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record ArtistDetailsResponse(
    Catalog.ArtistId ArtistId,
    string Name,
    IReadOnlyList<AlbumSummary> Albums);
