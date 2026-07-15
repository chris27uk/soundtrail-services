using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record MusicCatalogMetadataFetchedDto(
    string CommandId,
    string MusicCatalogId,
    string SourceProvider,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    SongMetadataDto? Metadata,
    IReadOnlyList<ExternalReferenceDto> References,
    IReadOnlyList<ProviderLookupFailureDto> FailedProviders,
    string? ArtistId,
    string? AlbumId,
    string CorrelationId);
