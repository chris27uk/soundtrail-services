namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogSearchRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string QueryText { get; set; } = string.Empty;

    public string Filter { get; set; } = string.Empty;

    public CatalogSearchResultRecordDto[] Results { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string normalizedSearchCriteria) => $"catalog/search/{normalizedSearchCriteria}";
}

public sealed class CatalogSearchResultRecordDto
{
    public string MusicCatalogId { get; set; } = string.Empty;

    public string ResultType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? ArtistName { get; set; }

    public string? AlbumTitle { get; set; }

    public string? ArtworkUrl { get; set; }
}
