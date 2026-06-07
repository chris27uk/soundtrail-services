using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class MusicTrackEventListener(MusicTrackEventCommandHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public object Handle(AppleMusicResolutionRequired @event, IAsyncDocumentSession _) => handler.Handle(@event);

    [WolverineHandler]
    [Transactional]
    public object Handle(YouTubeMusicResolutionRequired @event, IAsyncDocumentSession _) => handler.Handle(@event);
}
