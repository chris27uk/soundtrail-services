using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ArtistMetadataFetchedDto(
    string CommandId,
    string ArtistId,
    string SourceProvider,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    ArtistMetadataDto Metadata,
    string CorrelationId);
