using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

public sealed class CaptureProviderSnapshot(IProviderSnapshotStore snapshotStore)
{
    public Task CaptureAsync(
        Soundtrail.Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken) =>
        snapshotStore.SaveAsync(
            new ProviderSnapshot(
                response.MusicCatalogId,
                response.SourceProvider,
                response.CreatedAt,
                JsonSerializer.Serialize(ToDto(response))),
            cancellationToken);

    private static EnrichmentResponseDto ToDto(Soundtrail.Domain.Responses.EnrichmentResponse response) =>
        new(
            response.CommandId.Value,
            response.MusicCatalogId.Value,
            response.SourceProvider.Value,
            response.Priority,
            response.CreatedAt,
            response.Metadata is null
                ? null
                : new SongMetadataDto(
                    response.Metadata.Title,
                    response.Metadata.Artist,
                    response.Metadata.Isrc,
                    response.Metadata.Mbid,
                    response.Metadata.DurationMs),
            response.References.Select(reference => new ExternalReferenceDto(
                reference.Provider.Value,
                reference.Url,
                reference.ExternalId)).ToArray(),
            response.FailedProviders.Select(failure => new ProviderLookupFailureDto(
                failure.Provider.Value,
                failure.SourceProvider.Value)).ToArray(),
            response.Hierarchy?.ArtistId?.Value,
            response.Hierarchy?.AlbumId?.Value,
            response.CorrelationId.Value);
}
