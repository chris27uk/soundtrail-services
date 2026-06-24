using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicTrackIntegrationEventPublisherFake : IPublishMusicTrackIntegrationEvents
{
    public List<IReadOnlyCollection<MusicTrackIntegrationEvent>> PublishedBatches { get; } = [];

    public Task PublishAsync(
        IReadOnlyCollection<MusicTrackIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken)
    {
        PublishedBatches.Add(integrationEvents.ToArray());
        return Task.CompletedTask;
    }
}
