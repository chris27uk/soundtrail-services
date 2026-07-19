namespace Soundtrail.Domain.Catalog.Tracks;

public sealed record CanonicalTrackIdentityParts(
    string ArtistName,
    string TrackName,
    string? AlbumName,
    DateOnly? ReleaseDate,
    string? ReleaseType);
