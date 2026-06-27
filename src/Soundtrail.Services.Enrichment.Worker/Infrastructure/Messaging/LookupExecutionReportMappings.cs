using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

internal static class LookupExecutionReportMappings
{
    public static MusicCatalogLookupAttemptedDto ToDto(
        this MusicCatalogLookupAttempted attempted,
        IMusicCatalogLookupCommand command,
        string sourceProvider) =>
        new(
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
                    attempted.MusicCatalogMetadataFetched.CorrelationId.Value));
}
