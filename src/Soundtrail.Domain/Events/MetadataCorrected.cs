namespace Soundtrail.Domain.Events;

public sealed record MetadataCorrected(
    string Title,
    string ArtistName,
    string? ArtistId,
    string? AlbumTitle,
    string? AlbumId,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string Source,
    DateTimeOffset CorrectedAt) : IMusicTrackEvent;
