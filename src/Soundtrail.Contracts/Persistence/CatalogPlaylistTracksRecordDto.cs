namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogPlaylistTracksRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string PlaylistId { get; set; } = string.Empty;

    public CatalogPlaylistTrackRecordDto[] Tracks { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string playlistId) => $"catalog/playlist-tracks/{playlistId}";
}

public sealed class CatalogPlaylistTrackRecordDto
{
    public string TrackId { get; set; } = string.Empty;

    public string? TrackIdBaseKeyHigh { get; set; }

    public string? TrackIdBaseKeyLow { get; set; }

    public string? TrackIdSpecificKey { get; set; }

    public string MusicCatalogId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public int? DurationMs { get; set; }

    public string? Isrc { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ReleaseType { get; set; }

    public string? ArtworkUrl { get; set; }
}
