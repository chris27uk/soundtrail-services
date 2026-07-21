using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;

public sealed class LookupMusicbrainzAlbumTracksHandler(
    IReadTracksByAlbumIdPort readTracksByAlbumIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzAlbumTracksMessage>
{
    public async Task Handle(LookupMusicbrainzAlbumTracksMessage request, CancellationToken cancellationToken = default)
    {
        var entries = await readTracksByAlbumIdPort.ReadAsync(request.AlbumId, cancellationToken);
        var observedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new CatalogLookupCompleted(
                MessageId.New(),
                request.RequestedAt,
                request.CorrelationId,
                new LookupResult.Succeeded(
                    new LookupResultContext(
                        CatalogWorkId.From(new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId)),
                        request.Id),
                    new LookedUpData.CatalogEntries(entries),
                    observedAt)),
            cancellationToken);
    }
}
