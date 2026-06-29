using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record AlbumMetadataFetchedDto(
    string CommandId,
    string ArtistId,
    string AlbumId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    AlbumMetadataDto Metadata,
    string CorrelationId);
