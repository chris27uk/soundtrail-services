using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class MusicCatalogLookupAttemptedListener(MusicCatalogLookupAttemptedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        MusicCatalogLookupAttemptedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(
            new MusicCatalogLookupAttempted(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                ProviderName.From(dto.SourceProvider),
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
                        ProviderName.From(dto.MusicCatalogMetadataFetched.SourceProvider),
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
                            ProviderName.From(failure.SourceProvider))).ToArray(),
                        dto.MusicCatalogMetadataFetched.ArtistId is null && dto.MusicCatalogMetadataFetched.AlbumId is null
                            ? null
                            : new CatalogTrackHierarchy(
                                dto.MusicCatalogMetadataFetched.ArtistId is null ? null : ArtistId.From(dto.MusicCatalogMetadataFetched.ArtistId),
                                dto.MusicCatalogMetadataFetched.AlbumId is null ? null : AlbumId.From(dto.MusicCatalogMetadataFetched.AlbumId)),
                        CorrelationId.From(dto.MusicCatalogMetadataFetched.CorrelationId))),
            cancellationToken);
}
