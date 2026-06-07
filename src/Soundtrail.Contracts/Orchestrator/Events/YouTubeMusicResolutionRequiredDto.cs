namespace Soundtrail.Contracts.Orchestrator.Events;

public sealed record YouTubeMusicResolutionRequiredDto(
    string MusicCatalogId,
    LookupPriorityBand Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt);
