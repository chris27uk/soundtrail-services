using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ArtistMetadataLookupAttemptedDto(
    string CommandId,
    string ArtistId,
    string SourceProvider,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    ArtistMetadataFetchedDto? ArtistMetadataFetched);
