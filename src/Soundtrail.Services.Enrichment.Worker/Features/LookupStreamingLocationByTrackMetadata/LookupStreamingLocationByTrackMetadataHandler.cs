using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;

public sealed class LookupStreamingLocationByTrackMetadataHandler(
    IReadTrackForLookupPort readTrackForLookupPort,
    IReadStreamingLocationByProviderPort readStreamingLocationByProviderPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupStreamingLocationByTrackMetadataMessage>
{
    public async Task Handle(LookupStreamingLocationByTrackMetadataMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;
        var track = await readTrackForLookupPort.ReadAsync(request.TrackId, cancellationToken);

        if (track is null)
        {
            await commandBus.SendAsync(
                CreateCompleted(
                    request,
                    new LookupResult.Failed(
                        CreateContext(request),
                        "Track was not found for streaming lookup.",
                        observedAt)),
                cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(track.ArtistName) || string.IsNullOrWhiteSpace(track.Title))
        {
            await commandBus.SendAsync(
                CreateCompleted(
                    request,
                    new LookupResult.NotFound(
                        CreateContext(request),
                        "Track metadata is incomplete for provider lookup.",
                        observedAt)),
                cancellationToken);
            return;
        }

        var link = await readStreamingLocationByProviderPort.ReadByTrackMetadataAsync(
            track.ArtistName,
            track.Title,
            request.Provider,
            cancellationToken);

        if (link is null)
        {
            await commandBus.SendAsync(
                CreateCompleted(
                    request,
                    new LookupResult.NotFound(
                        CreateContext(request),
                        "Streaming location was not found for the requested provider.",
                        observedAt)),
                cancellationToken);
            return;
        }

        await commandBus.SendAsync(
            CreateCompleted(
                request,
                new LookupResult.Succeeded(
                    CreateContext(request),
                    new LookedUpData.TrackStreamingLink(
                        track.ArtistId,
                        track.TrackId,
                        new StreamingLocation(
                            request.Provider,
                            externalId: null,
                            link,
                            LookupSource.Odesli,
                            observedAt)),
                    observedAt)),
            cancellationToken);
    }

    private static CatalogLookupCompleted CreateCompleted(
        LookupStreamingLocationByTrackMetadataMessage request,
        LookupResult result) =>
        new(
            MessageId.New(),
            request.RequestedAt,
            request.CorrelationId,
            result);

    private static LookupResultContext CreateContext(LookupStreamingLocationByTrackMetadataMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(request.TrackId)),
            request.Id);
}
