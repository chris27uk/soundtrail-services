namespace Soundtrail.Contracts.Orchestrator.Commands;

public sealed record LookupMusicCommand(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId);
