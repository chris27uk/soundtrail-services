using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class MusicCatalogLookupAttemptedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<MusicCatalogLookupAttempted, MusicCatalogLookupAttemptedDto>(
            attempted =>
                new MusicCatalogLookupAttemptedDto(
                    attempted.CommandId.Value,
                    attempted.MusicCatalogId.Value,
                    attempted.SourceProvider.Value,
                    attempted.Priority,
                    attempted.CreatedAt,
                    attempted.CorrelationId.Value,
                    new MusicCatalogLookupOutcomeDto(
                        attempted.Outcome.Status.ToString(),
                        attempted.Outcome.Reason,
                        attempted.Outcome.RetryAt,
                        attempted.Outcome.RetryAfterSeconds),
                    attempted.MusicCatalogMetadataFetched is null
                        ? null
                        : new MusicCatalogMetadataFetchedDto(
                            attempted.MusicCatalogMetadataFetched.CommandId.Value,
                            attempted.MusicCatalogMetadataFetched.MusicCatalogId.Value,
                            attempted.MusicCatalogMetadataFetched.SourceProvider.Value,
                            attempted.MusicCatalogMetadataFetched.Priority,
                            attempted.MusicCatalogMetadataFetched.CreatedAt,
                            attempted.MusicCatalogMetadataFetched.Metadata is null
                                ? null
                                : new SongMetadataDto(
                                    attempted.MusicCatalogMetadataFetched.Metadata.Title,
                                    attempted.MusicCatalogMetadataFetched.Metadata.Artist,
                                    attempted.MusicCatalogMetadataFetched.Metadata.Isrc,
                                    attempted.MusicCatalogMetadataFetched.Metadata.Mbid,
                                    attempted.MusicCatalogMetadataFetched.Metadata.DurationMs,
                                    attempted.MusicCatalogMetadataFetched.Metadata.AlbumTitle,
                                    attempted.MusicCatalogMetadataFetched.Metadata.ReleaseDate,
                                    attempted.MusicCatalogMetadataFetched.Metadata.SourceArtistId,
                                    attempted.MusicCatalogMetadataFetched.Metadata.SourceAlbumId),
                            attempted.MusicCatalogMetadataFetched.References.Select(reference => new ExternalReferenceDto(
                                reference.Provider.Value,
                                reference.Url,
                                reference.ExternalId)).ToArray(),
                            attempted.MusicCatalogMetadataFetched.FailedProviders.Select(failure => new ProviderLookupFailureDto(
                                failure.Provider.Value,
                                failure.SourceProvider.Value)).ToArray(),
                            attempted.MusicCatalogMetadataFetched.Hierarchy?.ArtistId?.Value,
                            attempted.MusicCatalogMetadataFetched.Hierarchy?.AlbumId?.Value,
                            attempted.MusicCatalogMetadataFetched.CorrelationId.Value),
                    attempted.SearchCriteria is null
                        ? null
                        : DiscoveryQueryKey.StableValueFor(attempted.SearchCriteria)),
            dto =>
                new MusicCatalogLookupAttempted(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    LookupSource.From(dto.SourceProvider),
                    dto.Priority,
                    dto.CreatedAt,
                    CorrelationId.From(dto.CorrelationId),
                    new MusicCatalogLookupOutcome(
                        Enum.Parse<MusicCatalogLookupOutcomeStatus>(dto.Outcome.Status),
                        dto.Outcome.Reason,
                        dto.Outcome.RetryAt,
                        dto.Outcome.RetryAfterSeconds),
                    dto.MusicCatalogMetadataFetched is null
                        ? null
                        : new MusicCatalogMetadataFetched(
                            CommandId.From(dto.MusicCatalogMetadataFetched.CommandId),
                            MusicCatalogId.From(dto.MusicCatalogMetadataFetched.MusicCatalogId),
                            LookupSource.From(dto.MusicCatalogMetadataFetched.SourceProvider),
                            dto.MusicCatalogMetadataFetched.Priority,
                            dto.MusicCatalogMetadataFetched.CreatedAt,
                            dto.MusicCatalogMetadataFetched.Metadata is null
                                ? null
                                : new SongMetadata(
                                    dto.MusicCatalogMetadataFetched.Metadata.Title,
                                    dto.MusicCatalogMetadataFetched.Metadata.Artist,
                                    dto.MusicCatalogMetadataFetched.Metadata.Isrc,
                                    dto.MusicCatalogMetadataFetched.Metadata.Mbid,
                                    dto.MusicCatalogMetadataFetched.Metadata.DurationMs,
                                    dto.MusicCatalogMetadataFetched.Metadata.AlbumTitle,
                                    dto.MusicCatalogMetadataFetched.Metadata.ReleaseDate,
                                    dto.MusicCatalogMetadataFetched.Metadata.SourceArtistId,
                                    dto.MusicCatalogMetadataFetched.Metadata.SourceAlbumId),
                            dto.MusicCatalogMetadataFetched.References.Select(reference => new ExternalReference(
                                ProviderName.From(reference.Provider),
                                reference.Url,
                                reference.ExternalId)).ToArray(),
                            dto.MusicCatalogMetadataFetched.FailedProviders.Select(failure => new ProviderLookupFailure(
                                ProviderName.From(failure.Provider),
                                LookupSource.From(failure.SourceProvider))).ToArray(),
                            dto.MusicCatalogMetadataFetched.ArtistId is null && dto.MusicCatalogMetadataFetched.AlbumId is null
                                ? null
                                : new CatalogTrackHierarchy(
                                    dto.MusicCatalogMetadataFetched.ArtistId is null ? null : ArtistId.From(dto.MusicCatalogMetadataFetched.ArtistId),
                                    dto.MusicCatalogMetadataFetched.AlbumId is null ? null : AlbumId.From(dto.MusicCatalogMetadataFetched.AlbumId)),
                            CorrelationId.From(dto.MusicCatalogMetadataFetched.CorrelationId)),
                    string.IsNullOrWhiteSpace(dto.SearchCriteria)
                        ? null
                        : DiscoveryQueryKey.ToMusicSearchCriteria(dto.SearchCriteria)));
    }
}
