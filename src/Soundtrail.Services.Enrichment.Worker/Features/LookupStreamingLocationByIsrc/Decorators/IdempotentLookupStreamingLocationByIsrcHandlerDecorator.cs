using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;

public sealed class IdempotentLookupStreamingLocationByIsrcHandlerDecorator(
    IHandler<LookupStreamingLocationByIsrcMessage> inner,
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ICommandBus commandBus,
    IClockPort clock) : IHandler<LookupStreamingLocationByIsrcMessage>
{
    public async Task Handle(LookupStreamingLocationByIsrcMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;

        await using var session = await IdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            request.Id,
            cancellationToken);

        if (session.ProcessedBefore)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        CreateContext(request),
                        CreateExistingTrackItem(request.TrackId),
                        "Lookup already completed.",
                        observedAt)),
                cancellationToken);
            return;
        }

        try
        {
            await inner.Handle(request, cancellationToken);
            await session.CompleteAsync(cancellationToken);
        }
        catch (LookupExecutionShortCircuitException)
        {
            await session.ReleaseAsync(cancellationToken);
        }
        catch
        {
            await session.ReleaseAsync(cancellationToken);
            throw;
        }
    }

    private static LookupResultContext CreateContext(LookupStreamingLocationByIsrcMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(request.TrackId)),
            request.Id);

    private static CatalogItem CreateExistingTrackItem(TrackId trackId) =>
        new CatalogItem.MusicTrack(new Track(trackId));
}
