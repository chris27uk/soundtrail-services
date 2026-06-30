using Soundtrail.Contracts.IntegrationMessaging.Responses;

namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicCatalogLookupCompletedEventDataRecordDto(
    string MusicCatalogId,
    string SourceProvider,
    string Priority,
    DateTimeOffset CompletedAtUtc,
    SongMetadataDto? Metadata,
    IReadOnlyList<ExternalReferenceDto> References,
    IReadOnlyList<ProviderLookupFailureDto> FailedProviders,
    string? ArtistId,
    string? AlbumId,
    string? SearchCriteriaValue) : RavenEventBodyDto;
