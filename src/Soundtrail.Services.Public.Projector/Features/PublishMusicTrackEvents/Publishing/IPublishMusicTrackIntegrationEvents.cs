using Soundtrail.Domain.Catalog.IntegrationEvents;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

public interface IPublishMusicTrackIntegrationEvents
{
    Task PublishAsync(
        IReadOnlyCollection<MusicTrackIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken);
}
