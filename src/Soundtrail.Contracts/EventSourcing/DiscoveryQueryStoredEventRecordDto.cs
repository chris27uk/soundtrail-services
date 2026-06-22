namespace Soundtrail.Contracts.EventSourcing;

public sealed class DiscoveryQueryStoredEventRecordDto
{
    public const string AggregateTypeValue = "discovery-query";

    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public string AggregateType { get; set; } = AggregateTypeValue;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DiscoveryRequestedEventDataRecordDto? DiscoveryRequested { get; set; }

    public DiscoveryPlannedEventDataRecordDto? DiscoveryPlanned { get; set; }

    public DiscoveryDeferredEventDataRecordDto? DiscoveryDeferred { get; set; }

    public DiscoveryRejectedEventDataRecordDto? DiscoveryRejected { get; set; }

    public DiscoveryFailedEventDataRecordDto? DiscoveryFailed { get; set; }

    public DiscoveryStartedEventDataRecordDto? DiscoveryStarted { get; set; }

    public DiscoveryCompletedEventDataRecordDto? DiscoveryCompleted { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public static string GetDocumentId(string criteria, int version) =>
        $"discovery-query-events/{criteria}/{version:D10}";
}
