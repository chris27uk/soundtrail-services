using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ArtistMetadataLookupAttemptedDto(
    string CommandId,
    string ArtistId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    ArtistMetadataFetchedDto? ArtistMetadataFetched);
