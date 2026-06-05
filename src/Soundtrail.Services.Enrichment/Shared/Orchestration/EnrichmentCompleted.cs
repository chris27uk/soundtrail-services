using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Orchestration;

public sealed record EnrichmentCompleted(
    MusicCatalogId MusicCatalogId,
    CorrelationId CorrelationId) : IEnrichmentOrchestrationEvent;
