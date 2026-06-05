using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Orchestration;

public interface IEnrichmentOrchestrationEvent
{
    MusicCatalogId MusicCatalogId { get; }

    CorrelationId CorrelationId { get; }
}
