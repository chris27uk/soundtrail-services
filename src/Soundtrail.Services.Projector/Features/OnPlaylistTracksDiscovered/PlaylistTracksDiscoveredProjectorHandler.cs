using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;

public sealed class PlaylistTracksDiscoveredProjectorHandler(ICommandBus commandBus)
{
    public Task Handle(PlaylistTracksDiscovered @event, CancellationToken cancellationToken = default)
    {
        return commandBus.SendAsync(
            new PlaylistUpdated(@event.PlaylistId.Value, @event.Tracks)
            {
                CommandId = CommandId.For($"PlaylistUpdated:{@event.PlaylistId.Value}:{@event.ObservedAt:O}"),
                CorrelationId = CorrelationId.From($"playlist-tracks-discovered:{@event.PlaylistId.Value}:{@event.ObservedAt:O}"),
                CreatedAt = @event.ObservedAt
            },
            cancellationToken);
    }
}
