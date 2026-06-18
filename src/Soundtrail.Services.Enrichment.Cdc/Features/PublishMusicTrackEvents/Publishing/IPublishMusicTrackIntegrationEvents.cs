namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;

public interface IPublishMusicTrackIntegrationEvents
{
    Task PublishAsync(
        IReadOnlyCollection<object> integrationEvents,
        CancellationToken cancellationToken);
}
