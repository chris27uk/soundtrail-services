using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested;

public sealed class KnownTrackRequestedHandler(
    ILoadKnownTrackRequestedMusicTrackPort loadMusicTrackPort,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task Handle(
        KnownTrackRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (KnownTrackRequested)item.Event;
            var track = await loadMusicTrackPort.LoadAsync(@event.TrackId, cancellationToken);
            var history = await SearchOrSeekHistory.LoadAsync(
                discoveryRepository,
                command.KnownItem,
                cancellationToken);

            if (!track.AppendFollowUp(history, @event))
            {
                continue;
            }

            await history.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
