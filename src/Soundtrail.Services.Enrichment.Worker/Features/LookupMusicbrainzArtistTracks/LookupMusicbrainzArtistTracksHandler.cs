using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;

public sealed class LookupMusicbrainzArtistTracksHandler(
    IReadTracksByArtistIdPort readTracksByArtistIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzArtistTracksMessage>
{
    public async Task Handle(LookupMusicbrainzArtistTracksMessage request, CancellationToken cancellationToken = default)
    {
        var entries = await readTracksByArtistIdPort.ReadAsync(request.ArtistId, cancellationToken);
        var observedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new CatalogLookupCompleted(
                MessageId.New(),
                request.RequestedAt,
                request.CorrelationId,
                new LookupResult.Succeeded(
                    new LookupResultContext(
                        CatalogWorkId.From(new CatalogItemOperation.ChildTracksForArtist(request.ArtistId)),
                        request.Id),
                    new LookedUpData.CatalogEntries(entries),
                    observedAt)),
            cancellationToken);
    }
}
