using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class AlbumMetadataLookupAttemptedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<AlbumMetadataLookupAttempted, AlbumMetadataLookupAttemptedDto>(
            attempted => new AlbumMetadataLookupAttemptedDto(
                attempted.CommandId.Value,
                attempted.ArtistId.Value,
                attempted.AlbumId.Value,
                attempted.SourceProvider.Value,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.CorrelationId.Value,
                new MusicCatalogLookupOutcomeDto(
                    attempted.Outcome.Status.ToString(),
                    attempted.Outcome.Reason,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.RetryAfterSeconds),
                attempted.AlbumMetadataFetched is null
                    ? null
                    : new AlbumMetadataFetchedDto(
                        attempted.AlbumMetadataFetched.CommandId.Value,
                        attempted.AlbumMetadataFetched.ArtistId.Value,
                        attempted.AlbumMetadataFetched.AlbumId.Value,
                        attempted.AlbumMetadataFetched.SourceProvider.Value,
                        attempted.AlbumMetadataFetched.Priority,
                        attempted.AlbumMetadataFetched.CreatedAt,
                        new AlbumMetadataDto(
                            attempted.AlbumMetadataFetched.Metadata.AlbumTitle,
                            attempted.AlbumMetadataFetched.Metadata.ArtistName,
                            attempted.AlbumMetadataFetched.Metadata.SourceAlbumId,
                            attempted.AlbumMetadataFetched.Metadata.SourceArtistId,
                            attempted.AlbumMetadataFetched.Metadata.ReleaseDate),
                        attempted.AlbumMetadataFetched.CorrelationId.Value)),
            dto => new AlbumMetadataLookupAttempted(
                CommandId.From(dto.CommandId),
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                LookupSource.From(dto.SourceProvider),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                new MusicCatalogLookupOutcome(
                    Enum.Parse<MusicCatalogLookupOutcomeStatus>(dto.Outcome.Status),
                    dto.Outcome.Reason,
                    dto.Outcome.RetryAt,
                    dto.Outcome.RetryAfterSeconds),
                dto.AlbumMetadataFetched is null
                    ? null
                    : new AlbumMetadataFetched(
                        CommandId.From(dto.AlbumMetadataFetched.CommandId),
                        ArtistId.From(dto.AlbumMetadataFetched.ArtistId),
                        AlbumId.From(dto.AlbumMetadataFetched.AlbumId),
                        LookupSource.From(dto.AlbumMetadataFetched.SourceProvider),
                        dto.AlbumMetadataFetched.Priority,
                        dto.AlbumMetadataFetched.CreatedAt,
                        new AlbumMetadata(
                            dto.AlbumMetadataFetched.Metadata.AlbumTitle,
                            dto.AlbumMetadataFetched.Metadata.ArtistName,
                            dto.AlbumMetadataFetched.Metadata.SourceAlbumId,
                            dto.AlbumMetadataFetched.Metadata.SourceArtistId,
                            dto.AlbumMetadataFetched.Metadata.ReleaseDate),
                        CorrelationId.From(dto.AlbumMetadataFetched.CorrelationId))));
    }
}
