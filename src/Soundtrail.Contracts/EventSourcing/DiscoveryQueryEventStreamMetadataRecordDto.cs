namespace Soundtrail.Contracts.EventSourcing;

public sealed class DiscoveryQueryEventStreamMetadataRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public string AggregateType { get; set; } = DiscoveryQueryStoredEventRecordDto.AggregateTypeValue;

    public int Version { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static string GetDocumentId(string criteria) => $"discovery-query-streams/{criteria}";
}
