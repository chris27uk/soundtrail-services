namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogSearchCandidateRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string CatalogItemId { get; set; } = string.Empty;

    public string CandidateKind { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string catalogItemId) => $"catalog/search-candidates/{catalogItemId}";
}
