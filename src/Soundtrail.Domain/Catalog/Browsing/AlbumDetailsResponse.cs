namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record AlbumDetailsResponse(
    Catalog.ArtistId ArtistId,
    string ArtistName,
    Catalog.AlbumId AlbumId,
    string Name,
    DateOnly? ReleaseDate,
    IReadOnlyList<TrackSummary> Tracks);
