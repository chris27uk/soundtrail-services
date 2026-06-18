namespace Soundtrail.Contracts.EventSourcing;

public sealed record MetadataCorrectedEventDataRecordDto(
    string Title,
    string ArtistName,
    string? ArtistId,
    string? AlbumTitle,
    string? AlbumId,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string Source,
    DateTimeOffset CorrectedAt);
