using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicBrainzDumpFreshness;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;

public sealed class LookupMusicbrainzArtistAlbumsHandler(
    IMusicBrainzDumpFreshnessEvaluator dumpFreshnessEvaluator,
    IReadAlbumsByArtistIdPort readAlbumsByArtistIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzArtistAlbumsMessage>
{
    public async Task Handle(IncomingMessage<LookupMusicbrainzArtistAlbumsMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var observedAt = clock.UtcNow;
        var freshness = await dumpFreshnessEvaluator.EvaluateArtistAlbumsAsync(
            request.ArtistId,
            observedAt,
            cancellationToken);
        var entries = freshness.UseCatalog
            ? freshness.CatalogEntries
            : await readAlbumsByArtistIdPort.ReadAsync(request.ArtistId, cancellationToken);

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
