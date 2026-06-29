using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ArtistMetadataFetched(
    CommandId CommandId,
    ArtistId ArtistId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    ArtistMetadata Metadata,
    CorrelationId CorrelationId);
