using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record CatalogItemLookupAttemptedDto(
    string CommandId,
    CatalogItemKindDto ItemKindDto,
    string ItemValue,
    string SourceProvider,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    MusicCatalogMetadataFetchedDto? MusicCatalogMetadataFetched,
    ArtistMetadataFetchedDto? ArtistMetadataFetched,
    AlbumMetadataFetchedDto? AlbumMetadataFetched,
    string? SearchCriteria = null);
