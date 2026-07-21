using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;

public sealed class IdempotentLookupMusicbrainzAlbumTracksHandlerDecorator(
    IHandler<LookupMusicbrainzAlbumTracksMessage> inner,
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ICommandBus commandBus,
    IClockPort clock) : IHandler<LookupMusicbrainzAlbumTracksMessage>
{
    public async Task Handle(LookupMusicbrainzAlbumTracksMessage request, CancellationToken cancellationToken = default)
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
                        new CatalogItem.MusicAlbum(new Album(request.AlbumId, null, null, null, null, observedAt)),
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

    private static LookupResultContext CreateContext(LookupMusicbrainzAlbumTracksMessage request) =>
        new(CatalogWorkId.From(new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId)), request.Id);
}
