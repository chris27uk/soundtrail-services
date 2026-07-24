namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public sealed record CanonicalSongTitle(
    SongTitle CanonicalTrackTitle,
    ReleaseType? CanonicalReleaseType);
