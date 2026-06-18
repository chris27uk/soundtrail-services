using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Adapters;

public sealed class WolverineMusicTrackIntegrationEventPublisher(IMessageBus messageBus) : IPublishMusicTrackIntegrationEvents
{
    public async Task PublishAsync(
        IReadOnlyCollection<object> integrationEvents,
        CancellationToken cancellationToken)
    {
        foreach (var integrationEvent in integrationEvents)
        {
            await messageBus.SendAsync(integrationEvent);
        }
    }
}
