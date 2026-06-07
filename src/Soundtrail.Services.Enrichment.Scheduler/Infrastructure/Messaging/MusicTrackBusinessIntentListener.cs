using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Shared;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class MusicTrackBusinessIntentListener
{
    [WolverineHandler]
    public object Handle(AppleMusicResolutionRequired @event)
    {
        var command = new ResolveApplePlaybackReferenceCommand(
            CommandId.For($"ResolveApplePlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);

        return command.ToTransportMessage();
    }

    [WolverineHandler]
    public object Handle(YouTubeMusicResolutionRequired @event)
    {
        var command = new ResolveYouTubeMusicPlaybackReferenceCommand(
            CommandId.For($"ResolveYouTubeMusicPlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);

        return command.ToTransportMessage();
    }
}
