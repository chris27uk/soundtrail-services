using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class ArtistMetadataLookupAttemptedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<ArtistMetadataLookupAttempted, ArtistMetadataLookupAttemptedDto>(
            attempted => new ArtistMetadataLookupAttemptedDto(
                attempted.CommandId.Value,
                attempted.ArtistId.Value,
                attempted.SourceProvider.Value,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.CorrelationId.Value,
                new MusicCatalogLookupOutcomeDto(
                    attempted.Outcome.Status.ToString(),
                    attempted.Outcome.Reason,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.RetryAfterSeconds),
                attempted.ArtistMetadataFetched is null
                    ? null
                    : new ArtistMetadataFetchedDto(
                        attempted.ArtistMetadataFetched.CommandId.Value,
                        attempted.ArtistMetadataFetched.ArtistId.Value,
                        attempted.ArtistMetadataFetched.SourceProvider.Value,
                        attempted.ArtistMetadataFetched.Priority,
                        attempted.ArtistMetadataFetched.CreatedAt,
                        new ArtistMetadataDto(
                            attempted.ArtistMetadataFetched.Metadata.ArtistName,
                            attempted.ArtistMetadataFetched.Metadata.SourceArtistId),
                        attempted.ArtistMetadataFetched.CorrelationId.Value)),
            dto => new ArtistMetadataLookupAttempted(
                CommandId.From(dto.CommandId),
                ArtistId.From(dto.ArtistId),
                LookupSource.From(dto.SourceProvider),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                new MusicCatalogLookupOutcome(
                    Enum.Parse<MusicCatalogLookupOutcomeStatus>(dto.Outcome.Status),
                    dto.Outcome.Reason,
                    dto.Outcome.RetryAt,
                    dto.Outcome.RetryAfterSeconds),
                dto.ArtistMetadataFetched is null
                    ? null
                    : new ArtistMetadataFetched(
                        CommandId.From(dto.ArtistMetadataFetched.CommandId),
                        ArtistId.From(dto.ArtistMetadataFetched.ArtistId),
                        LookupSource.From(dto.ArtistMetadataFetched.SourceProvider),
                        dto.ArtistMetadataFetched.Priority,
                        dto.ArtistMetadataFetched.CreatedAt,
                        new ArtistMetadata(
                            dto.ArtistMetadataFetched.Metadata.ArtistName,
                            dto.ArtistMetadataFetched.Metadata.SourceArtistId),
                        CorrelationId.From(dto.ArtistMetadataFetched.CorrelationId))));
    }
}
