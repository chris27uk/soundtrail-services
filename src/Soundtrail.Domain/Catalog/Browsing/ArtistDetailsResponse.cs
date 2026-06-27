namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record ArtistDetailsResponse(
    ArtistId ArtistId,
    string Name,
    IReadOnlyList<AlbumSummary> Albums);
