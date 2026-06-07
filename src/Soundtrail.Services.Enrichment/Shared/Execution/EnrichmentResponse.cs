using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

public sealed record EnrichmentResponse(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    ProviderName SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    SongMetadata? Metadata,
    IReadOnlyList<ExternalReference> References,
    CorrelationId CorrelationId);
