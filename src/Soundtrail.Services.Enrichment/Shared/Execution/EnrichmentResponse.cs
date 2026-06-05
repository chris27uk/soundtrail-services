using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed record EnrichmentResponse(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    ProviderName SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    SongMetadata? Metadata,
    IReadOnlyList<ExternalReference> References,
    CorrelationId CorrelationId);
