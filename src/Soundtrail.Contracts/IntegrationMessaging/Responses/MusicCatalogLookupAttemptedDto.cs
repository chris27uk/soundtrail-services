using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record MusicCatalogLookupAttemptedDto(
    string CommandId,
    string MusicCatalogId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    MusicCatalogMetadataFetchedDto? MusicCatalogMetadataFetched,
    string? SearchCriteria = null);
