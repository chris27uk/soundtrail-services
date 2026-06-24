namespace Soundtrail.Contracts.Persistence;

internal sealed class RavenCatalogSearchTrackingRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string criteria) =>
        $"catalog-search-tracking/{Uri.EscapeDataString(criteria)}";
}
