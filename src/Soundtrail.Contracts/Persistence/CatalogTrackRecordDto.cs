namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogTrackRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public int? DurationMs { get; set; }

    public string? Isrc { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ReleaseType { get; set; }

    public string? ArtworkUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string trackId) => $"catalog/tracks/{trackId}";
}
