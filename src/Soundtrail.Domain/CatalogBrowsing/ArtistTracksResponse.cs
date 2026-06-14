namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record ArtistTracksResponse(
    Catalog.ArtistId ArtistId,
    string ArtistName,
    IReadOnlyList<TrackSummary> Tracks);
