using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents;

public sealed class PublishMusicTrackEventsHandler(
    IPublishMusicTrackIntegrationEvents publishMusicTrackIntegrationEvents) : IHandler<PublishMusicTrackEventsCommand>
{
    public async Task Handle(
        PublishMusicTrackEventsCommand request,
        CancellationToken cancellationToken = default)
    {
        var integrationEvents = request.Events
            .OrderBy(x => x.MusicCatalogId.Value, StringComparer.Ordinal)
            .ThenBy(x => x.Version)
            .Select(x => x.Event)
            .ToArray();

        if (integrationEvents.Length == 0)
        {
            return;
        }

        await publishMusicTrackIntegrationEvents.PublishAsync(integrationEvents, cancellationToken);
    }
}
