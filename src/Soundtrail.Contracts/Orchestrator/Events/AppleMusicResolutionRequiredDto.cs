namespace Soundtrail.Contracts.Orchestrator.Events;

public sealed record AppleMusicResolutionRequiredDto(
    string MusicCatalogId,
    LookupPriorityBand Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt);
