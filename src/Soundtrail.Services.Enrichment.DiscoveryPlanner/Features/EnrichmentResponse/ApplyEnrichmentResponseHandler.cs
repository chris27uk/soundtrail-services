using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
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
    IUpsertCatalogSearchStatusPort upsertDiscoveryStatusPort)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var stream = await eventRepository.LoadEventsAsync(response.MusicCatalogId, cancellationToken);
        var events = BuildEvents(stream, response);

        var append = await eventRepository.AppendEventsAsync(
            response.MusicCatalogId,
            stream.Version,
            response.CommandId,
            events,
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
            await upsertDiscoveryStatusPort.UpsertAsync(
                new CatalogSearchStatusUpdate(
                    tracking.Criteria,
                    CatalogSearchLifecycleStatus.Completed,
                    response.Priority,
                    WillBeLookedUp: false,
                    EstimatedRetryAfterSeconds: null,
                    EarliestExpectedCompletionAt: null,
                    Reason: "Discovery completed",
                    UpdatedAt: response.CreatedAt),
                cancellationToken);
        }

        return new EnrichmentOrchestrationResult(
            append.Appended ? append.AppendedEvents : Array.Empty<IMusicTrackEvent>());
    }

    private static IReadOnlyList<IMusicTrackEvent> BuildEvents(
        MusicTrackStream stream,
        Domain.Responses.EnrichmentResponse response)
    {
        var events = new List<IMusicTrackEvent>();

        if (response.SourceProvider == ProviderName.MusicBrainz && response.Metadata is not null)
        {
            if (response.Hierarchy?.ArtistId is not null || !string.IsNullOrWhiteSpace(response.Metadata.Artist))
            {
                events.Add(new ArtistDiscovered(
                    response.Hierarchy?.ArtistId?.Value,
                    response.Metadata.Artist,
                    response.SourceProvider,
                    response.CreatedAt));
            }

            if (response.Hierarchy?.AlbumId is not null)
            {
                events.Add(new AlbumDiscovered(
                    response.Hierarchy?.AlbumId?.Value,
                    null,
                    response.SourceProvider,
                    response.CreatedAt));
            }

            events.Add(new TrackDiscovered(
                response.Metadata.Title,
                response.Metadata.Artist,
                response.Metadata.DurationMs,
                response.Metadata.Isrc,
                response.Metadata.Mbid,
                response.SourceProvider,
                response.CreatedAt));
        }

        foreach (var reference in response.References)
        {
            events.Add(new ProviderReferenceDiscovered(
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                response.SourceProvider,
                response.CreatedAt));
        }

        foreach (var failedProvider in response.FailedProviders)
        {
            events.Add(new ProviderReferenceLookupFailed(
                failedProvider.Provider,
                failedProvider.SourceProvider,
                response.CreatedAt));
        }

        if (response.SourceProvider == ProviderName.MusicBrainz
            && response.Metadata is not null
            && !stream.Events.OfType<ProviderReferenceDiscovered>().Any()
            && !stream.Events.OfType<PlaybackReferencesResolutionRequired>().Any())
        {
            var searchTerm = !string.IsNullOrWhiteSpace(response.Metadata.Isrc)
                ? MusicSearchTerm.ByIsrc(response.Metadata.Isrc)
                : MusicSearchTerm.ByTrackArtistAlbum(
                    response.Metadata.Title,
                    response.Metadata.Artist,
                    album: null);

            events.Add(new PlaybackReferencesResolutionRequired(
                response.MusicCatalogId,
                response.Priority,
                response.CorrelationId,
                response.SourceProvider,
                response.CreatedAt,
                searchTerm,
                response.Hierarchy));
        }

        return events;
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
