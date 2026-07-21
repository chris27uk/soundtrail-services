namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogSearchCandidateRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string CatalogItemId { get; set; } = string.Empty;

    public string CandidateKind { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? ArtistName { get; set; }

    public string? AlbumTitle { get; set; }

    public string? ArtworkUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string catalogItemId) => $"catalog/search-candidates/{catalogItemId}";
}
