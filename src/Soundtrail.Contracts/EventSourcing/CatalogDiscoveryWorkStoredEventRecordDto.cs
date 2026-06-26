namespace Soundtrail.Contracts.EventSourcing;

public sealed class CatalogDiscoveryWorkStoredEventRecordDto
{
    public const string AggregateTypeValue = "catalog-discovery-work";

    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = AggregateTypeValue;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public CatalogDiscoveryWorkRequestedEventDataRecordDto? CatalogDiscoveryWorkRequested { get; set; }

    public CatalogDiscoveryWorkDeferredEventDataRecordDto? CatalogDiscoveryWorkDeferred { get; set; }

    public CatalogDiscoveryWorkIgnoredEventDataRecordDto? CatalogDiscoveryWorkIgnored { get; set; }

    public CatalogDiscoveryWorkScheduledEventDataRecordDto? CatalogDiscoveryWorkScheduled { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public static string GetDocumentId(string musicCatalogId, int version) =>
        $"catalog-discovery-work-events/{musicCatalogId}/{version:D10}";
}
