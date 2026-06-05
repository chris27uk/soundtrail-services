using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Orchestration;

public sealed record ProviderReferenceDiscovered(
    MusicCatalogId MusicCatalogId,
    ProviderName Provider,
    string? ExternalId,
    ProviderName SourceProvider,
    CorrelationId CorrelationId) : IEnrichmentOrchestrationEvent;
