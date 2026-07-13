namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogArtistTracksRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public CatalogArtistTrackRecordDto[] Tracks { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string artistId) => $"catalog/artist-tracks/{artistId}";
}

public sealed class CatalogArtistTrackRecordDto
{
    public string TrackId { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public int? DurationMs { get; set; }

    public string? Isrc { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ArtworkUrl { get; set; }
}
