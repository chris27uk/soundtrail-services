using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ArtistMetadataFetchedDto(
    string CommandId,
    string ArtistId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    ArtistMetadataDto Metadata,
    string CorrelationId);
