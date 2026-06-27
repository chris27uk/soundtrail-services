using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record MusicCatalogMetadataFetched(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    ProviderName SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    SongMetadata? Metadata,
    IReadOnlyList<ExternalReference> References,
    IReadOnlyList<ProviderLookupFailure> FailedProviders,
    CatalogTrackHierarchy? Hierarchy,
    CorrelationId CorrelationId);
