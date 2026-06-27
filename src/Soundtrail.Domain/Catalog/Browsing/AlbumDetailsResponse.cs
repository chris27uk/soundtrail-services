namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record AlbumDetailsResponse(
    ArtistId ArtistId,
    string ArtistName,
    AlbumId AlbumId,
    string Name,
    DateOnly? ReleaseDate,
    IReadOnlyList<TrackSummary> Tracks);
