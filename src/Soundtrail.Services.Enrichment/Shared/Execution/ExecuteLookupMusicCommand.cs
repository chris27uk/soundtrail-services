using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed record ExecuteLookupMusicCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    ProviderName Provider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId);
