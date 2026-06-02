namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record LookupMusicCommand(
    string CommandId,
    MusicCatalogId MusicCatalogId,
    DateTimeOffset CreatedAt,
    string CorrelationId);
