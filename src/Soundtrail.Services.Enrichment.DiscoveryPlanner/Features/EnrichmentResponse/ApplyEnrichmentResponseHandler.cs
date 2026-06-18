using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IMusicTrackEventRepository eventRepository,
    IProviderSnapshotStore snapshotStore,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await CatalogEntityAggregate.LoadAsync(
            eventRepository,
            response.MusicCatalogId,
            cancellationToken);
        aggregate.RecordEnrichmentResponse(response);
        var append = await aggregate.SaveAsync(
            eventRepository,
            response.CommandId,
            cancellationToken);

        await snapshotStore.SaveAsync(
            new ProviderSnapshot(
                response.MusicCatalogId,
                response.SourceProvider,
                response.CreatedAt,
                JsonSerializer.Serialize(ToDto(response))),
            cancellationToken);

        foreach (var criteria in CatalogSearchCriteriaSet.ForResolvedTrack(
                     response.MusicCatalogId,
                     response.Hierarchy?.ArtistId,
                     response.Hierarchy?.AlbumId))
        {
            await catalogSearchTrackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    criteria,
                    response.MusicCatalogId,
                    response.CreatedAt),
                cancellationToken);
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(response.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);
            discovery.Complete(response.Priority, "Discovery completed", response.CreatedAt);
            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }

        return new EnrichmentOrchestrationResult(
            append.Appended ? append.AppendedEvents : Array.Empty<IMusicTrackEvent>());
    }

    private static EnrichmentResponseDto ToDto(Domain.Responses.EnrichmentResponse response) =>
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
