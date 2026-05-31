using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record LookupMusicCommand(
    string CommandId,
    MusicCatalogId MusicCatalogId,
    NormalizedSearchQuery Query,
    DateTimeOffset CreatedAt,
    string CorrelationId);
