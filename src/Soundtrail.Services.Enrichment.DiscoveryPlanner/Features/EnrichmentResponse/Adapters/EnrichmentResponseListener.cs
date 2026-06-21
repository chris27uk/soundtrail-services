using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class EnrichmentResponseListener(ApplyEnrichmentResponseHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        EnrichmentResponseDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        await handler.Handle(
            new Soundtrail.Domain.Responses.EnrichmentResponse(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                ProviderName.From(dto.SourceProvider),
                dto.Priority,
                dto.CreatedAt,
                dto.Metadata is null
                    ? null
                    : new SongMetadata(
                        dto.Metadata.Title,
                        dto.Metadata.Artist,
                        dto.Metadata.Isrc,
                        dto.Metadata.Mbid,
                        dto.Metadata.DurationMs,
                        dto.Metadata.AlbumTitle,
                        dto.Metadata.ReleaseDate),
                dto.References.Select(reference => new ExternalReference(
                    ProviderName.From(reference.Provider),
                    reference.Url,
                    reference.ExternalId)).ToArray(),
                dto.FailedProviders.Select(failure => new ProviderLookupFailure(
                    ProviderName.From(failure.Provider),
                    ProviderName.From(failure.SourceProvider))).ToArray(),
                dto.ArtistId is null && dto.AlbumId is null
                    ? null
                    : new CatalogTrackHierarchy(
                        dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                        dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId)),
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);

        return [];
    }
}
