using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Adapters.MusicBrainzDumpFreshness;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;

public sealed class LookupMusicbrainzAlbumTracksHandler(
    IMusicBrainzDumpFreshnessEvaluator dumpFreshnessEvaluator,
    IReadTracksByAlbumIdPort readTracksByAlbumIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzAlbumTracksMessage>
{
    public async Task Handle(IncomingMessage<LookupMusicbrainzAlbumTracksMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var observedAt = clock.UtcNow;
        var freshness = await dumpFreshnessEvaluator.EvaluateAlbumTracksAsync(
            request.AlbumId,
            observedAt,
            cancellationToken);
        var entries = freshness.UseCatalog
            ? freshness.CatalogEntries
            : await readTracksByAlbumIdPort.ReadAsync(request.AlbumId, cancellationToken);

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
