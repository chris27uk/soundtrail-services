using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;

public sealed class LookupPlaylistTracksByProviderHandler(
    IReadPlaylistTracksByProviderPort readPlaylistTracksByProviderPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupPlaylistTracksByProviderMessage>
{
    public async Task Handle(IncomingMessage<LookupPlaylistTracksByProviderMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var trackReferences = await readPlaylistTracksByProviderPort.ReadAsync(
            request.PlaylistId,
            request.Provider,
            cancellationToken);
        var observedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new CatalogLookupCompleted(
                MessageId.New(),
                request.RequestedAt,
                request.CorrelationId,
                new LookupResult.Succeeded(
                    CreateContext(request),
                    new LookedUpData.PlaylistTrackReferences(trackReferences),
                    observedAt)),
            cancellationToken);
    }

    private static LookupResultContext CreateContext(LookupPlaylistTracksByProviderMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId)),
            request.Id);
}
