namespace Soundtrail.Contracts.Orchestrator.Commands;

public sealed record LookupMusicCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId);
