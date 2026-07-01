using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record MetadataCorrected(
    MusicCatalogId? MusicCatalogId,
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
    DateTimeOffset CorrectedAt) : IMusicTrackEvent
{
    public MetadataCorrected(
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
        DateTimeOffset CorrectedAt)
        : this(
            null,
            Title,
            ArtistName,
            ArtistId,
            SourceArtistId,
            AlbumTitle,
            AlbumId,
            SourceAlbumId,
            ReleaseDate,
            DurationMs,
            Isrc,
            Mbid,
            Source,
            CorrectedAt)
    {
    }
}
