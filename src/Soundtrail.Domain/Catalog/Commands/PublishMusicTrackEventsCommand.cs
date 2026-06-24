using Soundtrail.Domain.Catalog.IntegrationEvents;

namespace Soundtrail.Domain.Commands;

public sealed record PublishMusicTrackEventsCommand(
    IReadOnlyCollection<VersionedMusicTrackIntegrationEvent> Events);
