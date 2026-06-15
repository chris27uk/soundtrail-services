using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Responses;

public sealed record EnrichmentResponse(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    ProviderName SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    SongMetadata? Metadata,
    IReadOnlyList<ExternalReference> References,
    IReadOnlyList<ProviderLookupFailure> FailedProviders,
    CorrelationId CorrelationId);
