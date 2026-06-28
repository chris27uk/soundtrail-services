using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested;

public sealed class KnownTrackRequestedHandler(
    ILoadKnownTrackRequestedMusicTrackPort loadMusicTrackPort,
    ICommandBus commandBus)
{
    public async Task Handle(
        KnownTrackRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (KnownTrackRequested)item.Event;
            var track = await loadMusicTrackPort.LoadAsync(@event.TrackId, cancellationToken);
            var lookupCommand = track.CreateLookupCommand(@event);
            if (lookupCommand is null)
            {
                continue;
            }

            await commandBus.SendAsync(lookupCommand, cancellationToken);
        }
    }
}
