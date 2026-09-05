namespace Soundtrail.Contracts.Persistence;

/// <summary>
/// Durable fold of an artist-catalog stream used to avoid full event replay on re-projection.
/// </summary>
public sealed class ArtistCatalogProjectionSnapshotDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public int StreamVersion { get; set; }

    public string ArtistName { get; set; } = string.Empty;

    public string? ArtworkUrl { get; set; }

    public string? MusicBrainzArtistId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ArtistCatalogProjectionSnapshotAlbumDto[] Albums { get; set; } = [];

    public ArtistCatalogProjectionSnapshotTrackDto[] Tracks { get; set; } = [];

    public static string GetDocumentId(string artistId) =>
        $"artist-catalog-projection-snapshots/{artistId}";
}

public sealed class ArtistCatalogProjectionSnapshotAlbumDto
{
    public string AlbumId { get; set; } = string.Empty;

    public string AlbumTitle { get; set; } = string.Empty;

    public string? SourceAlbumId { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ArtworkUrl { get; set; }
}

public sealed class ArtistCatalogProjectionSnapshotTrackDto
{
    public string TrackId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? AlbumId { get; set; }

    public string? AlbumTitle { get; set; }

    public int? DurationMs { get; set; }

    public string? Isrc { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ReleaseType { get; set; }

    public string? ArtworkUrl { get; set; }

    public ArtistCatalogProjectionSnapshotStreamingLocationDto[] StreamingLocations { get; set; } = [];
}

public sealed class ArtistCatalogProjectionSnapshotStreamingLocationDto
{
    public string Provider { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string Url { get; set; } = string.Empty;
}
