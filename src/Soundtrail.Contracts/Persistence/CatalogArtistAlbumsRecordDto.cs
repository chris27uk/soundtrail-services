namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogArtistAlbumsRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public CatalogArtistAlbumRecordDto[] Albums { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string artistId) => $"catalog/artist-albums/{artistId}";
}

public sealed class CatalogArtistAlbumRecordDto
{
    public string AlbumId { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AlbumTitle { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    public string? ArtworkUrl { get; set; }
}
