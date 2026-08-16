namespace Soundtrail.Contracts.EventSourcing;

public sealed class RavenStoredEventRecord
{
    public string Id { get; set; } = string.Empty;

    public string StreamId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = string.Empty;

    public int Version { get; set; }

    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string BodyType { get; set; } = string.Empty;

    public RavenEventBodyDto? Body { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    /// <summary>
    /// <c>live</c> (default) or <c>bulk-import</c>. Live CDC subscriptions exclude bulk-import.
    /// </summary>
    public string ProjectionHint { get; set; } = "live";
}
