using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;
using Wolverine;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Adapters;

public sealed class WolverineMusicTrackIntegrationEventPublisher(IMessageBus messageBus) : IPublishMusicTrackIntegrationEvents
{
    public async Task PublishAsync(
        IReadOnlyCollection<MusicTrackIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken)
    {
        foreach (var integrationEvent in integrationEvents)
        {
            await messageBus.SendAsync(integrationEvent.ToMessage());
        }
    }
}
