namespace Soundtrail.Contracts.EventSourcing;

public sealed record MetadataCorrectedEventDataRecordDto(
    string? MusicCatalogId,
    string Title,
    string ArtistName,
    string? ArtistId,
    string? SourceArtistId,
    string? AlbumTitle,
    string? AlbumId,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string Source,
    DateTimeOffset CorrectedAt) : RavenEventBodyDto;
