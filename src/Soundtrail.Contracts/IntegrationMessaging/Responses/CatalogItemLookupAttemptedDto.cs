using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record CatalogItemLookupAttemptedDto(
    string CommandId,
    CatalogItemKind ItemKind,
    string ItemValue,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicCatalogLookupOutcomeDto Outcome,
    MusicCatalogMetadataFetchedDto? MusicCatalogMetadataFetched,
    ArtistMetadataFetchedDto? ArtistMetadataFetched,
    AlbumMetadataFetchedDto? AlbumMetadataFetched,
    string? SearchCriteria = null);
