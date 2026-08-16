using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicBrainzDumpFreshness;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;

public sealed class LookupMusicbrainzArtistTracksHandler(
    IMusicBrainzDumpFreshnessEvaluator dumpFreshnessEvaluator,
    IReadTracksByArtistIdPort readTracksByArtistIdPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzArtistTracksMessage>
{
    public async Task Handle(IncomingMessage<LookupMusicbrainzArtistTracksMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var observedAt = clock.UtcNow;
        var freshness = await dumpFreshnessEvaluator.EvaluateArtistTracksAsync(
            request.ArtistId,
            observedAt,
            cancellationToken);
        var entries = freshness.UseCatalog
            ? freshness.CatalogEntries
            : await readTracksByArtistIdPort.ReadAsync(request.ArtistId, cancellationToken);

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
