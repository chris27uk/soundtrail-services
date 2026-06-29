namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters.Documents;

public sealed class CatalogSearchTrackingRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string criteria) =>
        $"catalog-search-tracking/{Uri.EscapeDataString(criteria)}";
}
