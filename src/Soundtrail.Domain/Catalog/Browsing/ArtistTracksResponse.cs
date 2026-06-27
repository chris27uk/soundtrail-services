namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record ArtistTracksResponse(
    ArtistId ArtistId,
    string ArtistName,
    IReadOnlyList<TrackSummary> Tracks);
