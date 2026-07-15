using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record AlbumMetadataLookupAttemptedDto(
    string CommandId,
    string ArtistId,
    string AlbumId,
    string SourceProvider,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    AlbumMetadataFetchedDto? AlbumMetadataFetched);
