namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record MusicCatalogMatchEvidence(
    bool IsExactIdentityMatch,
    string? NormalizedTitle,
    string? NormalizedArtist,
    string? NormalizedAlbumTitle,
    string? NormalizedIsrc,
    string? NormalizedMusicBrainzRecordingId,
    DateOnly? ReleaseDate)
{
    public static MusicCatalogMatchEvidence None { get; } =
        new(
            false,
            null,
            null,
            null,
            null,
            null,
            null);
}
