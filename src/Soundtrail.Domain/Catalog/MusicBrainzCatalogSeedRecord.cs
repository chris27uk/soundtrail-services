namespace Soundtrail.Domain.Catalog;

public sealed record MusicBrainzCatalogSeedRecord(
    string SourceRecordKey,
    string SourceTrackId,
    string Title,
    string Artist,
    string? SourceArtistId,
    string? AlbumTitle,
    string? SourceAlbumId,
    string? Isrc,
    string? MusicBrainzRecordingId,
    int? DurationMs,
    DateOnly? ReleaseDate);
