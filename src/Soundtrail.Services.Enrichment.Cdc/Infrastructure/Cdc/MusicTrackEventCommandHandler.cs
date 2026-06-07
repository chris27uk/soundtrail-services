using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

public sealed class MusicTrackEventCommandHandler
{
    public object Handle(AppleMusicResolutionRequired @event)
    {
        var command = new ResolveApplePlaybackReferenceCommand(
            CommandId.For($"ResolveApplePlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);

        return command.Priority == LookupPriorityBand.High
            ? command
            : command;
    }

    public object Handle(YouTubeMusicResolutionRequired @event)
    {
        var command = new ResolveYouTubeMusicPlaybackReferenceCommand(
            CommandId.For($"ResolveYouTubeMusicPlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);

        return command.Priority == LookupPriorityBand.High
            ? command
            : command;
    }
}
