namespace Soundtrail.Contracts.EventSourcing;

public sealed class MusicTrackStoredEventRecordDto
{
    public const string AggregateTypeValue = "music-track";

    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = AggregateTypeValue;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int SchemaVersion { get; set; } = 1;

    public string BodyJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public static string GetDocumentId(string musicCatalogId, int version) => $"music-track-events/{musicCatalogId}/{version:D10}";
}
