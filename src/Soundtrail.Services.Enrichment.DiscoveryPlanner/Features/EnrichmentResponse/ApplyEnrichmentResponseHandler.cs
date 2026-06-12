using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.ApplyResponse;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IMusicTrackEventRepository eventRepository,
    IMusicTrackProjectionStore projectionStore,
    IProviderSnapshotStore snapshotStore)
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

        var updatedStream = await eventRepository.LoadEventsAsync(response.MusicCatalogId, cancellationToken);
        await projectionStore.StoreAsync(response.MusicCatalogId, updatedStream, cancellationToken);

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
            events.Add(new MinimalTrackInfoDiscovered(
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
            events.Add(new ProviderPlaybackReferenceResolved(
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                response.SourceProvider,
                response.CreatedAt));
        }

        if (response.SourceProvider == ProviderName.MusicBrainz
            && response.Metadata is not null
            && !stream.Events.OfType<ProviderPlaybackReferenceResolved>().Any()
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
                searchTerm));
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
            response.CorrelationId.Value);
}
