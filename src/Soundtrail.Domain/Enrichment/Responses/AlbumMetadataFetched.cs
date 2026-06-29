using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record AlbumMetadataFetched(
    CommandId CommandId,
    ArtistId ArtistId,
    AlbumId AlbumId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    AlbumMetadata Metadata,
    CorrelationId CorrelationId);
