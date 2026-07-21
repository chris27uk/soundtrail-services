using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;

public sealed class IdempotentLookupPlaylistTracksByProviderHandlerDecorator(
    IHandler<LookupPlaylistTracksByProviderMessage> inner,
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ICommandBus commandBus,
    IClockPort clock) : IHandler<LookupPlaylistTracksByProviderMessage>
{
    public async Task Handle(LookupPlaylistTracksByProviderMessage request, CancellationToken cancellationToken = default)
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
                        CreateExistingPlaylistItem(),
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

    private static LookupResultContext CreateContext(LookupPlaylistTracksByProviderMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId)),
            request.Id);

    private static CatalogItem CreateExistingPlaylistItem() =>
        new CatalogItem.MusicPlaylist(new Playlist());
}
