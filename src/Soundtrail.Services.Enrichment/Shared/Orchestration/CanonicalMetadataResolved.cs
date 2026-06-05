using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Orchestration;

public sealed record CanonicalMetadataResolved(
    MusicCatalogId MusicCatalogId,
    SongMetadata Metadata,
    CorrelationId CorrelationId) : IEnrichmentOrchestrationEvent;
