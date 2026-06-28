using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;
using Soundtrail.Adapters.Registry;
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
            object message;
            try
            {
                message = TypeTranslationRegistry.Default.ToDto(integrationEvent);
            }
            catch (InvalidOperationException)
            {
                message = integrationEvent;
            }

            await messageBus.SendAsync(message);
        }
    }
}
