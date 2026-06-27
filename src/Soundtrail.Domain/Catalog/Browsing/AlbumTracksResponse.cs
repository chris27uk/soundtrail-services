namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record AlbumTracksResponse(
    ArtistId ArtistId,
    string ArtistName,
    AlbumId AlbumId,
    string AlbumName,
    IReadOnlyList<TrackSummary> Tracks);
