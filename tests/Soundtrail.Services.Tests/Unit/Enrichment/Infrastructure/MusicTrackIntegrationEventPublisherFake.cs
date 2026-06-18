using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicTrackIntegrationEventPublisherFake : IPublishMusicTrackIntegrationEvents
{
    public List<IReadOnlyCollection<object>> PublishedBatches { get; } = [];

    public Task PublishAsync(
        IReadOnlyCollection<object> integrationEvents,
        CancellationToken cancellationToken)
    {
        PublishedBatches.Add(integrationEvents.ToArray());
        return Task.CompletedTask;
    }
}
