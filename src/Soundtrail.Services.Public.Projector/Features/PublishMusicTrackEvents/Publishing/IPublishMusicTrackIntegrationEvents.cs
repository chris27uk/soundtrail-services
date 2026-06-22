namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

public interface IPublishMusicTrackIntegrationEvents
{
    Task PublishAsync(
        IReadOnlyCollection<object> integrationEvents,
        CancellationToken cancellationToken);
}
