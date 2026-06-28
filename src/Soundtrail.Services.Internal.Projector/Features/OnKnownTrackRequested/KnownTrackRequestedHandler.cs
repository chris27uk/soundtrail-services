using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested;

public sealed class KnownTrackRequestedHandler(
    ILoadKnownTrackRequestedMusicTrackPort loadMusicTrackPort,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository)
{
    public async Task Handle(
        KnownTrackRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (KnownTrackRequested)item.Event;
            var track = await loadMusicTrackPort.LoadAsync(@event.TrackId, cancellationToken);
            await SearchOrSeekHistory.ApplyAsync(
                discoveryRepository,
                command.KnownItem,
                history => track.AppendFollowUp(history, @event),
                cancellationToken);
        }
    }
}
