namespace Soundtrail.Contracts.EventSourcing;

public sealed class RavenEventStreamMetadataRecord
{
    public string Id { get; set; } = string.Empty;

    public string StreamId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<string> AppliedOperationIds { get; set; } = [];

    public Dictionary<string, string> Properties { get; set; } = [];
}
