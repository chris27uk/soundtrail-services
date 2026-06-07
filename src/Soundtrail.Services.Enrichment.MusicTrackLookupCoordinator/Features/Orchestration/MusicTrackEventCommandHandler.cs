using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;

public sealed class MusicTrackEventCommandHandler
{
    public ResolveApplePlaybackReferenceCommand Handle(AppleMusicResolutionRequired @event) =>
        new(
            CommandId.For($"ResolveApplePlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);

    public ResolveYouTubeMusicPlaybackReferenceCommand Handle(YouTubeMusicResolutionRequired @event) =>
        new(
            CommandId.For($"ResolveYouTubeMusicPlaybackReference:{@event.MusicCatalogId.Value}"),
            @event.MusicCatalogId,
            @event.Priority,
            @event.ObservedAt,
            @event.CorrelationId);
}
