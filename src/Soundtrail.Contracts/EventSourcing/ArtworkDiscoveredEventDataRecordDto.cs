namespace Soundtrail.Contracts.EventSourcing;

public sealed record ArtworkDiscoveredEventDataRecordDto(
    string EntityKind,
    string? EntityId,
    string Url,
    string Source,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
