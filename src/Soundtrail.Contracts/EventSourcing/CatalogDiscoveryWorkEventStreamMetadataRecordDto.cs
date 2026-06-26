namespace Soundtrail.Contracts.EventSourcing;

public sealed class CatalogDiscoveryWorkEventStreamMetadataRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = CatalogDiscoveryWorkStoredEventRecordDto.AggregateTypeValue;

    public int Version { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static string GetDocumentId(string musicCatalogId) => $"catalog-discovery-work-streams/{musicCatalogId}";
}
