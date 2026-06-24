namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record AlbumTracksResponse(
    Catalog.ArtistId ArtistId,
    string ArtistName,
    Catalog.AlbumId AlbumId,
    string AlbumName,
    IReadOnlyList<TrackSummary> Tracks);
