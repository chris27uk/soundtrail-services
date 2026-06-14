namespace Soundtrail.Contracts.EventSourcing;

public sealed class DiscoveryQueryStoredEventRecordDto
{
    public const string AggregateTypeValue = "discovery-query";

    public string Id { get; set; } = string.Empty;

    public string QueryKey { get; set; } = string.Empty;

    public string AggregateType { get; set; } = AggregateTypeValue;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public static string GetDocumentId(string queryKey, int version) =>
        $"discovery-query-events/{queryKey}/{version:D10}";
}
