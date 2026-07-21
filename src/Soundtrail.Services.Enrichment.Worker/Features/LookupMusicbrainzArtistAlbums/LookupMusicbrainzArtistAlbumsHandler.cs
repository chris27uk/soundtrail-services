using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;

public sealed class LookupMusicbrainzArtistAlbumsHandler(
    IReadAlbumsByArtistIdPort readAlbumsByArtistIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzArtistAlbumsMessage>
{
    public async Task Handle(LookupMusicbrainzArtistAlbumsMessage request, CancellationToken cancellationToken = default)
    {
        var entries = await readAlbumsByArtistIdPort.ReadAsync(request.ArtistId, cancellationToken);
        var observedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new CatalogLookupCompleted(
                MessageId.New(),
                request.RequestedAt,
                request.CorrelationId,
                new LookupResult.Succeeded(
                    new LookupResultContext(
                        CatalogWorkId.From(new CatalogItemOperation.ChildAlbumsForArtist(request.ArtistId)),
                        request.Id),
                    new LookedUpData.CatalogEntries(entries),
                    observedAt)),
            cancellationToken);
    }
}
